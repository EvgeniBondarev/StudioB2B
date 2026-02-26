using Microsoft.AspNetCore.Http;
using StudioB2B.Application.Common.Interfaces;
using StudioB2B.Infrastructure.Features;
using StudioB2B.Infrastructure.Persistence.Master;
using StudioB2B.Infrastructure.Services;

namespace StudioB2B.Infrastructure.MultiTenancy;

public class TenantMiddleware
{
    private readonly RequestDelegate _next;

    public TenantMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        TenantProvider tenantProvider,
        MasterDbContext masterDb,
        ISubdomainResolver subdomainResolver)
    {
        var subdomain = subdomainResolver.Resolve(context.Request.Host.Host);

        if (!string.IsNullOrEmpty(subdomain))
        {
            var tenant = await masterDb.Tenants
                             .GetByPredicateAsync(t => t.Subdomain == subdomain && t.IsActive, context.RequestAborted);

            if (tenant != null)
            {
                tenantProvider.SetTenant(tenant);
            }
            else
            {
                // Строим URL мастер-домена из реального scheme/host запроса,
                // убирая субдомен (demo.localhost → localhost, demo.studiob2b.ru → studiob2b.ru)
                var host = context.Request.Host.Host;
                var masterHost = host.Contains('.')
                    ? host[(host.IndexOf('.') + 1)..]
                    : host;

                var port = context.Request.Host.Port;
                var hostWithPort = port.HasValue && port.Value is not (80 or 443)
                    ? $"{masterHost}:{port.Value}"
                    : masterHost;

                var scheme = context.Request.Scheme;
                context.Response.Redirect($"{scheme}://{hostWithPort}/");
                return;
            }
        }

        await _next(context);
    }
}
