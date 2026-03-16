using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StudioB2B.Domain.Entities;
using StudioB2B.Domain.Options;
using StudioB2B.Infrastructure.Helpers;
using StudioB2B.Infrastructure.Interfaces;
using StudioB2B.Infrastructure.Persistence.Master;

namespace StudioB2B.Infrastructure.Services.MultiTenancy;

public class TenantMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantMiddleware> _logger;

    public TenantMiddleware(RequestDelegate next, ILogger<TenantMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, TenantProvider tenantProvider, MasterDbContext masterDb,
                                  ISubdomainResolver subdomainResolver, IOptions<MultiTenancyOptions> options)
    {
        var subdomain = subdomainResolver.Resolve(context.Request.Host);

        _logger.LogInformation("Request {Path}, Host: {Host}, Subdomain: {Subdomain}",
            context.Request.Path, context.Request.Host.Value, subdomain ?? "null");

        if (!string.IsNullOrEmpty(subdomain))
        {
            var tenant = await FindActiveTenantAsync(masterDb, subdomain, context.RequestAborted);

            if (tenant != null)
            {
                tenantProvider.SetTenant(tenant);
                context.Response.AppendLastTenantCookie(tenant.Subdomain, options.Value.MasterDomain);
            }
            else
            {
                _logger.LogWarning("No active tenant for '{Subdomain}', redirecting to master", subdomain);
                var masterUrl = BuildMasterUrl(context, options.Value.MasterDomain);
                context.Response.Redirect(masterUrl);
                return;
            }
        }
        else if (await TryRedirectToLastTenantAsync(context, masterDb, options.Value))
        {
            return;
        }

        await _next(context);
    }

    private static async Task<TenantEntity?> FindActiveTenantAsync(MasterDbContext masterDb, string subdomain, CancellationToken ct)
    {
        return await masterDb.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Subdomain == subdomain && t.IsActive, ct);
    }

    /// <summary>
    /// Строит URL master-домена, сохраняя схему (http/https) и нестандартный порт из текущего запроса.
    /// </summary>
    private static string BuildMasterUrl(HttpContext context, string masterDomain)
    {
        var scheme = context.Request.Scheme;
        var normalizedDomain = DomainHelper.Normalize(masterDomain);
        var port = context.Request.Host.Port;
        var portStr = port.HasValue && port.Value != 80 && port.Value != 443
            ? $":{port.Value}"
            : string.Empty;
        return $"{scheme}://{normalizedDomain}{portStr}/";
    }

    private async Task<bool> TryRedirectToLastTenantAsync(
        HttpContext context, MasterDbContext masterDb, MultiTenancyOptions options)
    {
        var lastTenant = context.Request.GetLastTenant();
        if (string.IsNullOrEmpty(lastTenant))
            return false;

        var exists = await masterDb.Tenants
            .AnyAsync(t => t.Subdomain == lastTenant && t.IsActive, context.RequestAborted);

        if (!exists)
            return false;

        var path = context.Request.Path + context.Request.QueryString;
        var redirectUrl = lastTenant.GetTenantUrl(options.MasterDomain, path);

        _logger.LogInformation("Redirecting to last tenant: {RedirectUrl}", redirectUrl);
        context.Response.Redirect(redirectUrl);
        return true;
    }
}
