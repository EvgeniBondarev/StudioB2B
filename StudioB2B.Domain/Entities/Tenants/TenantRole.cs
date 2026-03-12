using StudioB2B.Domain.Entities.Common;

namespace StudioB2B.Domain.Entities.Tenants;

public class TenantRole : IBaseEntity, ISoftDelete
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public bool IsDeleted { get; set; }

    public ICollection<TenantUserRole> UserRoles { get; set; } = new List<TenantUserRole>();
}

