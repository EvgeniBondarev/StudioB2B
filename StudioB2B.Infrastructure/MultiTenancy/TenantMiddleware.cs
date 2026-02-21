using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StudioB2B.Infrastructure.Persistence.Master;
using StudioB2B.Infrastructure.Services;

namespace StudioB2B.Infrastructure.MultiTenancy;

/// <summary>
/// Middleware для определения тенанта по субдомену
/// Должен быть зарегистрирован ДО Authentication/Authorization
/// </summary>
public class TenantMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantMiddleware> _logger;

    public TenantMiddleware(RequestDelegate next, ILogger<TenantMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        TenantProvider tenantProvider,
        MasterDbContext masterDb,
        IOptions<MultiTenancyOptions> options)
    {
        _logger.LogInformation("TenantMiddleware processing request: {Path}", context.Request.Path);
        _logger.LogInformation("Host: {Host}", context.Request.Host.Value);

        var subdomain = ResolveSubdomain(context.Request.Host, options.Value, _logger);
        _logger.LogInformation("Resolved subdomain: {Subdomain}", subdomain ?? "null");

        if (!string.IsNullOrEmpty(subdomain))
        {
            await ResolveTenantAsync(subdomain, tenantProvider, masterDb, context.RequestAborted);

            if (tenantProvider.IsResolved)
            {
                // Сохраняем последний тенант в куку
                context.Response.AppendLastTenantCookie(tenantProvider.Subdomain!, options.Value.MasterDomain);
            }
        }
        else
        {
            // Главный домен - пробуем редирект на последний тенант
            var lastTenant = context.Request.GetLastTenant();

            if (!string.IsNullOrEmpty(lastTenant))
            {
                var tenant = await masterDb.Tenants
                                 .FirstOrDefaultAsync(t => t.Subdomain == lastTenant && t.IsActive,
                                                      context.RequestAborted);

                if (tenant != null)
                {
                    var redirectUrl = lastTenant.GetTenantUrl(options.Value.MasterDomain,
                                                              context.Request.Path + context.Request.QueryString);

                    _logger.LogInformation("Redirecting to last tenant: {RedirectUrl}", redirectUrl);
                    context.Response.Redirect(redirectUrl);
                    return;
                }
            }

            _logger.LogWarning("No subdomain resolved from host: {Host}", context.Request.Host.Value);
        }
        await _next(context);
    }

    private static string? ResolveSubdomain(HostString host, MultiTenancyOptions options, ILogger logger)
    {
        var hostValue = host.Host;
        var originalHost = host.Value;

        // Подробное логирование для диагностики
        logger.LogInformation("[TENANT DEBUG] ===== ResolveSubdomain =====");
        logger.LogInformation("[TENANT DEBUG] Original Host: {OriginalHost}", originalHost);
        logger.LogInformation("[TENANT DEBUG] Host.Host: {HostValue}", hostValue);
        logger.LogInformation("[TENANT DEBUG] MasterDomain from config: '{MasterDomain}'", options.MasterDomain);
        logger.LogInformation("[TENANT DEBUG] ReservedSubdomains: {ReservedSubdomains}", string.Join(", ", options.ReservedSubdomains));

        // Проверка на localhost
        if (hostValue.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase))
        {
            var subdomain = hostValue[..^".localhost".Length];
            logger.LogInformation("[TENANT DEBUG] Localhost detected, subdomain: '{Subdomain}'", subdomain);

            if (!string.IsNullOrEmpty(subdomain) &&
                !options.ReservedSubdomains.Contains(subdomain, StringComparer.OrdinalIgnoreCase))
            {
                logger.LogInformation("[TENANT DEBUG] ✅ Localhost subdomain resolved: '{Subdomain}'", subdomain);
                return subdomain.ToLowerInvariant();
            }
        }

        // Проверка на точное совпадение с мастер-доменом (без субдомена)
        if (string.Equals(hostValue, options.MasterDomain, StringComparison.OrdinalIgnoreCase))
        {
            logger.LogInformation("[TENANT DEBUG] Exact match with MasterDomain, no subdomain");
            return null;
        }

        // Извлекаем субдомен из host: demo.studiob2b.ru -> demo
        var masterDomain = options.MasterDomain?.TrimStart('.') ?? "";
        logger.LogInformation("[TENANT DEBUG] Checking if '{HostValue}' ends with '{MasterDomain}'", hostValue, masterDomain);

        if (!string.IsNullOrEmpty(masterDomain) && hostValue.EndsWith(masterDomain, StringComparison.OrdinalIgnoreCase))
        {
            // Вычисляем префикс (субдомен)
            var prefix = hostValue[..^masterDomain.Length].TrimEnd('.');
            logger.LogInformation("[TENANT DEBUG] Found ends with master domain, prefix: '{Prefix}'", prefix);

            if (!string.IsNullOrEmpty(prefix))
            {
                // Проверка на зарезервированные субдомены
                if (options.ReservedSubdomains.Contains(prefix, StringComparer.OrdinalIgnoreCase))
                {
                    logger.LogInformation("[TENANT DEBUG] ⚠️ Subdomain '{Prefix}' is reserved, ignoring", prefix);
                    return null;
                }

                logger.LogInformation("[TENANT DEBUG] ✅ Subdomain resolved: '{Prefix}'", prefix);
                return prefix.ToLowerInvariant();
            }
            else
            {
                logger.LogInformation("[TENANT DEBUG] Empty prefix - this is the main domain");
            }
        }
        else
        {
            logger.LogInformation("[TENANT DEBUG] Host does not end with master domain");
            logger.LogInformation("[TENANT DEBUG] Host ends with '.studiob2b.ru'? {EndsWithDot}",
                hostValue.EndsWith(".studiob2b.ru", StringComparison.OrdinalIgnoreCase));
            logger.LogInformation("[TENANT DEBUG] Host ends with 'studiob2b.ru'? {EndsWith}",
                hostValue.EndsWith("studiob2b.ru", StringComparison.OrdinalIgnoreCase));
        }

        // Последняя попытка: если host содержит ".studiob2b.ru", пробуем извлечь первую часть
        if (hostValue.Contains(".studiob2b.ru") && !hostValue.StartsWith("www."))
        {
            var parts = hostValue.Split('.');
            if (parts.Length >= 3)
            {
                var possibleSubdomain = parts[0];
                logger.LogInformation("[TENANT DEBUG] Fallback: trying first part '{PossibleSubdomain}' as subdomain", possibleSubdomain);

                if (!options.ReservedSubdomains.Contains(possibleSubdomain, StringComparer.OrdinalIgnoreCase))
                {
                    logger.LogInformation("[TENANT DEBUG] ✅ Fallback resolved: '{PossibleSubdomain}'", possibleSubdomain);
                    return possibleSubdomain.ToLowerInvariant();
                }
            }
        }

        logger.LogInformation("[TENANT DEBUG] ❌ No subdomain resolved for: {HostValue}", hostValue);
        return null;
    }

    private async Task ResolveTenantAsync(
        string subdomain,
        TenantProvider tenantProvider,
        MasterDbContext masterDb,
        CancellationToken cancellationToken)
    {
        var tenant = await masterDb.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Subdomain == subdomain && t.IsActive, cancellationToken);

        if (tenant != null)
        {
            tenantProvider.SetTenant(tenant);
            _logger.LogInformation("Tenant resolved: {TenantId} ({Subdomain})", tenant.Id, subdomain);
        }
        else
        {
            _logger.LogWarning("Tenant not found for subdomain: {Subdomain}", subdomain);
        }
    }
}

/// <summary>
/// Extension methods для регистрации TenantMiddleware
/// </summary>
public static class TenantMiddlewareExtensions
{
    public static IApplicationBuilder UseTenantResolution(this IApplicationBuilder app)
    {
        return app.UseMiddleware<TenantMiddleware>();
    }
}
