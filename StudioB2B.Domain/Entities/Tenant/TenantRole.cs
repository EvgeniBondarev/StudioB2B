using StudioB2B.Domain.Entities.Common;

namespace StudioB2B.Domain.Entities.Tenant;

public class TenantRole : IHasId, IHasName, ISoftDelete
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public bool IsDeleted { get; set; }

    public List<TenantUser> Users { get; set; } = [];
}
