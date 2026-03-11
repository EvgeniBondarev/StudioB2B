using StudioB2B.Domain.Entities.Common;

namespace StudioB2B.Domain.Entities.Master;

public class MasterRole : IBaseEntity, ISoftDelete
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public bool IsDeleted { get; set; }

    public ICollection<MasterUserRole> UserRoles { get; set; } = new List<MasterUserRole>();
}

