using StudioB2B.Domain.Entities;
using StudioB2B.Infrastructure.Interfaces;

namespace StudioB2B.Infrastructure.Services;


public class TenantProvider : ITenantProvider
{
    public Guid? TenantId { get; private set; }

    public string? Subdomain { get; private set; }

    public string? ConnectionString { get; private set; }

    public bool IsResolved => TenantId.HasValue && !string.IsNullOrEmpty(ConnectionString);

    public void SetTenant(TenantEntity tenantEntity)
    {
        TenantId = tenantEntity.Id;
        Subdomain = tenantEntity.Subdomain;
        ConnectionString = tenantEntity.ConnectionString;
    }
}
