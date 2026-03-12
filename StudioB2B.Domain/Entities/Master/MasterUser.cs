using StudioB2B.Domain.Entities.Common;

namespace StudioB2B.Domain.Entities.Master;

public class MasterUser : IBaseEntity, ISoftDelete
{
    public Guid Id { get; set; }

    public string Email { get; set; } = null!;

    public string HashPassword { get; set; } = null!;

    public bool IsActive { get; set; } = true;

    public bool IsDeleted { get; set; }

    public ICollection<MasterUserRole> UserRoles { get; set; } = new List<MasterUserRole>();
}

