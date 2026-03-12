namespace StudioB2B.Domain.Entities;

public class TenantRole : IBaseEntity, ISoftDelete
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public bool IsDeleted { get; set; }

    public ICollection<TenantUserRole> UserRoles { get; set; } = new List<TenantUserRole>();
}

