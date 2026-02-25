using StudioB2B.Domain.Entities.Common;

namespace StudioB2B.Domain.Entities.Tenants;

public class TenantEntity : IHasId, IHasName, ISoftDelete
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Subdomain { get; set; } = string.Empty;

    public string ConnectionString { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public bool IsDeleted { get; set; }
}
