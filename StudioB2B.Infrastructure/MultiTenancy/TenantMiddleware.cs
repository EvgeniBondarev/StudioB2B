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
        var subdomain = ResolveSubdomain(context.Request.Host, options.Value);

        if (!string.IsNullOrEmpty(subdomain))
        {
            await ResolveTenantAsync(subdomain, tenantProvider, masterDb, context.RequestAborted);
        }

        await _next(context);
    }

    private static string? ResolveSubdomain(HostString host, MultiTenancyOptions options)
    {
        var hostValue = host.Host;

        // demo.localhost:port -> субдомен "demo"
        if (hostValue.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase))
        {
            var subdomain = hostValue[..^".localhost".Length];
            if (!string.IsNullOrEmpty(subdomain) &&
                !options.ReservedSubdomains.Contains(subdomain, StringComparer.OrdinalIgnoreCase))
            {
                return subdomain.ToLowerInvariant();
            }
        }

        // Чистый localhost без субдомена — тенант не определяется, показывается страница регистрации

        // Извлекаем субдомен из host: demo.studiob2b.com -> demo
        var masterDomain = options.MasterDomain;
        if (hostValue.EndsWith(masterDomain, StringComparison.OrdinalIgnoreCase))
        {
            var prefix = hostValue[..^masterDomain.Length].TrimEnd('.');
            if (!string.IsNullOrEmpty(prefix) && !options.ReservedSubdomains.Contains(prefix, StringComparer.OrdinalIgnoreCase))
            {
                return prefix.ToLowerInvariant();
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
            Console.WriteLine($"Tenant resolved: {tenant.Id} ({subdomain})");
            _logger.LogDebug("Tenant resolved: {TenantId} ({Subdomain})", tenant.Id, subdomain);
        }
        else
        {
            _logger.LogDebug("Tenant not found for subdomain: {Subdomain}", subdomain);
            Console.WriteLine($"Tenant not found for subdomain: {subdomain}");
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
