using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using StudioB2B.Application.Common.Interfaces;
using StudioB2B.Domain.Constants;
using StudioB2B.Domain.Extensions;
using StudioB2B.Domain.Options;
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
        ISubdomainResolver subdomainResolver,
        IOptions<MultiTenancyOptions> options)
    {
        var subdomain = subdomainResolver.Resolve(context.Request.Host.Host);

        if (!string.IsNullOrEmpty(subdomain))
        {
            var tenant =  await masterDb.Tenants
                              .GetByPredicateAsync(t => t.Subdomain == subdomain && t.IsActive, context.RequestAborted);

            if (tenant != null)
            {
                tenantProvider.SetTenant(tenant);
            }
            else
            {
                var masterUrl = $"{WebConstants.HttpsScheme}{options.Value.MasterDomain.NormalizeDomain()}/";
                context.Response.Redirect(masterUrl);
                return;
            }
        }

        await _next(context);
    }
}
