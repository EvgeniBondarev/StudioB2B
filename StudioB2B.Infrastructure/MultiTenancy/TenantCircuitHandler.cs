using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StudioB2B.Infrastructure.Persistence.Master;
using StudioB2B.Infrastructure.Services;

namespace StudioB2B.Infrastructure.MultiTenancy;

/// <summary>
/// Circuit handler для заполнения TenantProvider в Blazor Server circuits
/// </summary>
public class TenantCircuitHandler : CircuitHandler
{
    private readonly TenantProvider _tenantProvider;
    private readonly MasterDbContext _masterDb;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<TenantCircuitHandler> _logger;

    public TenantCircuitHandler(
        TenantProvider tenantProvider,
        MasterDbContext masterDb,
        IHttpContextAccessor httpContextAccessor,
        ILogger<TenantCircuitHandler> logger)
    {
        _tenantProvider = tenantProvider;
        _masterDb = masterDb;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public override async Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        // Если тенант уже определён (например, middleware успел это сделать) — пропускаем
        if (_tenantProvider.IsResolved)
        {
            _logger.LogDebug("Circuit {CircuitId} tenant already resolved: {TenantId}", circuit.Id, _tenantProvider.TenantId);
            return;
        }

        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            _logger.LogDebug("No HttpContext available for circuit {CircuitId}", circuit.Id);
            return;
        }

        var host = httpContext.Request.Host;
        var subdomain = ResolveSubdomain(host);

        if (string.IsNullOrEmpty(subdomain))
        {
            _logger.LogDebug("No subdomain resolved for circuit {CircuitId}, host: {Host}", circuit.Id, host);
            return;
        }

        var tenant = await _masterDb.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Subdomain == subdomain && t.IsActive, cancellationToken);

        if (tenant == null)
        {
            _logger.LogDebug("Tenant not found for subdomain {Subdomain} in circuit {CircuitId}", subdomain, circuit.Id);
            return;
        }

        _tenantProvider.SetTenant(tenant);
        _logger.LogDebug("Circuit {CircuitId} tenant set: {TenantId} ({Subdomain})", circuit.Id, tenant.Id, subdomain);
    }

    private static string? ResolveSubdomain(HostString host)
    {
        var hostValue = host.Host;

        // demo.localhost -> demo
        if (hostValue.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase))
        {
            return hostValue[..^".localhost".Length].ToLowerInvariant();
        }

        // demo.studiob2b.com -> demo
        var parts = hostValue.Split('.');
        if (parts.Length >= 2)
        {
            return parts[0].ToLowerInvariant();
        }

        return null;
    }
}
