using StudioB2B.Domain.Entities.Common;

namespace StudioB2B.Domain.Entities.Master;

public class MasterUser : IHasId, ISoftDelete
{
    public Guid Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public bool IsDeleted { get; set; }

    public List<MasterRole> Roles { get; set; } = [];
}
