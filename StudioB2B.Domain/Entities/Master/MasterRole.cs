using StudioB2B.Domain.Entities.Common;

namespace StudioB2B.Domain.Entities.Master;

public class MasterRole : IHasId, IHasName, ISoftDelete
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public bool IsDeleted { get; set; }

    public List<MasterUser> Users { get; set; } = [];
}
