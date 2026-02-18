using StudioB2B.Application.Common.Interfaces;
using TenantEntity = StudioB2B.Domain.Entities.Tenants.Tenant;

namespace StudioB2B.Infrastructure.Services;

/// <summary>
/// Провайдер информации о текущем тенанте (Scoped)
/// Заполняется в TenantMiddleware
/// </summary>
public class TenantProvider : ITenantProvider
{
    public Guid? TenantId { get; private set; }
    public string? Subdomain { get; private set; }
    public string? ConnectionString { get; private set; }
    public bool IsResolved => TenantId.HasValue && !string.IsNullOrEmpty(ConnectionString);

    public void SetTenant(TenantEntity tenant)
    {
        TenantId = tenant.Id;
        Subdomain = tenant.Subdomain;
        ConnectionString = tenant.ConnectionString;
    }

    public void Clear()
    {
        TenantId = null;
        Subdomain = null;
        ConnectionString = null;
    }
}
