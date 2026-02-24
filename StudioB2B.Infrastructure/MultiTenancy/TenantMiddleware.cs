using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StudioB2B.Infrastructure.Persistence.Master;
using StudioB2B.Infrastructure.Services;

namespace StudioB2B.Infrastructure.MultiTenancy;

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
                context.Response.AppendLastTenantCookie(tenantProvider.Subdomain!, options.Value.MasterDomain);
            }
            else
            {
                var master = options.Value.MasterDomain
                                      .TrimStart("https://".ToCharArray())
                                      .TrimStart("http://".ToCharArray())
                                      .TrimEnd('/');
                var redirect = $"https://{master}/";
                _logger.LogWarning("No tenant for '{Subdomain}', redirecting to {Redirect}", subdomain, redirect);
                context.Response.Redirect(redirect);
                return;
            }
        }
        else
        {
            var lastTenant = context.Request.GetLastTenant();

            if (!string.IsNullOrEmpty(lastTenant))
            {
                var tenant = await masterDb.Tenants
                                 .FirstOrDefaultAsync(t => t.Subdomain == lastTenant && t.IsActive,
                                                      context.RequestAborted);

                if (tenant != null)
                {
                    var originalPath = context.Request.Path + context.Request.QueryString;
                    if (originalPath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                        originalPath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogWarning("TenantMiddleware received absolute path '{OriginalPath}', resetting to '/'", originalPath);
                        originalPath = "/";
                    }

                    var redirectUrl = lastTenant.GetTenantUrl(options.Value.MasterDomain, originalPath);

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
        if (hostValue.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase))
        {
            var subdomain = hostValue[..^".localhost".Length];

            if (!string.IsNullOrEmpty(subdomain) &&
                !options.ReservedSubdomains.Contains(subdomain, StringComparer.OrdinalIgnoreCase))
            {
                return subdomain.ToLowerInvariant();
            }
        }

        if (string.Equals(hostValue, options.MasterDomain, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        // Извлекаем субдомен из host: demo.studiob2b.ru -> demo
        var masterDomain = options.MasterDomain.TrimStart('.');

        logger.LogInformation("[TENANT DEBUG] hostvalue -  {HostValue}", hostValue);
        logger.LogInformation("[TENANT DEBUG] masterdomain -  {MasterDomain}", masterDomain);

        if (!string.IsNullOrEmpty(masterDomain) && hostValue.EndsWith(masterDomain, StringComparison.OrdinalIgnoreCase))
        {
            var prefix = hostValue[..^masterDomain.Length].TrimEnd('.');

            if (!string.IsNullOrEmpty(prefix))
            {
                if (options.ReservedSubdomains.Contains(prefix, StringComparer.OrdinalIgnoreCase))
                {
                    return null;
                }

                return prefix.ToLowerInvariant();
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
                if (!options.ReservedSubdomains.Contains(possibleSubdomain, StringComparer.OrdinalIgnoreCase))
                {
                    return possibleSubdomain.ToLowerInvariant();
                }
            }
        }
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
        }
        else
        {
            _logger.LogWarning("Tenant not found for subdomain: {Subdomain}", subdomain);
        }
    }
}
