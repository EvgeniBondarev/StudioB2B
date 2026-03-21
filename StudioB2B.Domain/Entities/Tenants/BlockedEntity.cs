using StudioB2B.Domain.Constants;

namespace StudioB2B.Domain.Entities;

/// <summary>
/// Ограничение доступа к конкретному экземпляру сущности в рамках права.
/// Если для права заданы записи типа X — пользователь видит ТОЛЬКО эти экземпляры.
/// Если записей нет — доступны все экземпляры данного типа.
/// </summary>
public class BlockedEntity : IBaseEntity
{
    public Guid Id { get; set; }

    public BlockedEntityTypeEnum EntityType { get; set; }

    /// <summary>Id разрешённого экземпляра сущности.</summary>
    public Guid EntityId { get; set; }

    public Guid PermissionId { get; set; }
    public Permission Permission { get; set; } = null!;
}

