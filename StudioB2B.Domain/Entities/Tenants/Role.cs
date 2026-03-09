using StudioB2B.Domain.Entities.Common;

namespace StudioB2B.Domain.Entities.Tenants;

public class Role : IBaseEntity, ISoftDelete
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public bool IsDeleted { get; set; }

    // Navigation
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}

