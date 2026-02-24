using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StudioB2B.Infrastructure.MultiTenancy.Resolution;
using StudioB2B.Infrastructure.Persistence.Master;
using StudioB2B.Infrastructure.Services;

namespace StudioB2B.Infrastructure.MultiTenancy.CircuitHandlers;
//Circuit — это постоянное соединение между браузером пользователя и сервером через SignalR (WebSocket).
/*
1. HTTP-запрос       → TenantMiddleware срабатывает ✅
2. WebSocket открыт  → Circuit создан
3. Пользователь кликает по UI
4. Всё идёт через WebSocket → Middleware НЕ вызывается ❌
TenantMiddleware работает только на HTTP-запросы. После установки WebSocket-соединения middleware больше не вызывается — поэтому нужен TenantCircuitHandler
*/
public class TenantCircuitHandler : CircuitHandler
{
    private readonly TenantProvider _tenantProvider;
    private readonly MasterDbContext _masterDb;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ISubdomainResolver _subdomainResolver;
    private readonly ILogger<TenantCircuitHandler> _logger;

    public TenantCircuitHandler(
        TenantProvider tenantProvider,
        MasterDbContext masterDb,
        IHttpContextAccessor httpContextAccessor,
        ISubdomainResolver subdomainResolver,
        ILogger<TenantCircuitHandler> logger)
    {
        _tenantProvider = tenantProvider;
        _masterDb = masterDb;
        _httpContextAccessor = httpContextAccessor;
        _subdomainResolver = subdomainResolver;
        _logger = logger;
    }

    public override async Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        if (_tenantProvider.IsResolved)
        {
            _logger.LogDebug("Circuit {CircuitId} tenant already resolved: {TenantId}",
                circuit.Id, _tenantProvider.TenantId);
            return;
        }

        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            _logger.LogDebug("No HttpContext for circuit {CircuitId}", circuit.Id);
            return;
        }

        var subdomain = _subdomainResolver.Resolve(httpContext.Request.Host);
        if (string.IsNullOrEmpty(subdomain))
        {
            _logger.LogDebug("No subdomain for circuit {CircuitId}, host: {Host}",
                circuit.Id, httpContext.Request.Host);
            return;
        }

        var tenant = await _masterDb.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Subdomain == subdomain && t.IsActive, cancellationToken);

        if (tenant == null)
        {
            _logger.LogDebug("Tenant not found for {Subdomain} in circuit {CircuitId}",
                subdomain, circuit.Id);
            return;
        }

        _tenantProvider.SetTenant(tenant);
        _logger.LogDebug("Circuit {CircuitId} tenant set: {TenantId} ({Subdomain})",
            circuit.Id, tenant.Id, subdomain);
    }
}
