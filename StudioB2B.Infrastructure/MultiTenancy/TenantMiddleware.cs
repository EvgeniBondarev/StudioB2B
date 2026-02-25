using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StudioB2B.Application.Common.Interfaces;
using StudioB2B.Domain.Constants;
using StudioB2B.Domain.Entities.Tenants;
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
                context.Response.AppendLastTenantCookie(tenant.Subdomain, options.Value.MasterDomain);
            }
            else
            {
                var masterUrl = $"{WebConstants.HttpsScheme}{options.Value.MasterDomain.NormalizeDomain()}/";
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

    private static async Task<bool> TryRedirectToLastTenantAsync(
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

        context.Response.Redirect(redirectUrl);
        return true;
    }
}
