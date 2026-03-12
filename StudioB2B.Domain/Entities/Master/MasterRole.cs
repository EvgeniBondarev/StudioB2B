namespace StudioB2B.Domain.Entities;

public class MasterRole : IBaseEntity, ISoftDelete
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public bool IsDeleted { get; set; }

    public ICollection<MasterUserRole> UserRoles { get; set; } = new List<MasterUserRole>();
}

