using StudioB2B.Domain.Entities.Common;

namespace StudioB2B.Domain.Entities.Tenants;

public class MasterRole : IBaseEntity, ISoftDelete
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string NormalizedName { get; set; } = null!;

    public string ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString();

    public string? Description { get; set; }

    public bool IsSystemRole { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public bool IsDeleted { get; set; }
}

