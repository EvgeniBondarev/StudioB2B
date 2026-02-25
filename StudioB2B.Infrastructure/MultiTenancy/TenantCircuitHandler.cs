using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using StudioB2B.Application.Common.Interfaces;
using StudioB2B.Infrastructure.Features;
using StudioB2B.Infrastructure.Persistence.Master;
using StudioB2B.Infrastructure.Services;

namespace StudioB2B.Infrastructure.MultiTenancy;
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

    public TenantCircuitHandler(
        TenantProvider tenantProvider,
        MasterDbContext masterDb,
        IHttpContextAccessor httpContextAccessor,
        ISubdomainResolver subdomainResolver)
    {
        _tenantProvider = tenantProvider;
        _masterDb = masterDb;
        _httpContextAccessor = httpContextAccessor;
        _subdomainResolver = subdomainResolver;
    }

    public override async Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (_tenantProvider.IsResolved || httpContext == null)
            return;

        var subdomain = _subdomainResolver.Resolve(httpContext.Request.Host.Host);
        if (string.IsNullOrEmpty(subdomain))
            return;

        var tenant = await _masterDb.Tenants
                         .GetByPredicateAsync(t => t.Subdomain == subdomain && t.IsActive, cancellationToken);

        if (tenant == null)
            return;

        _tenantProvider.SetTenant(tenant);
    }
}
