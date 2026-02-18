namespace StudioB2B.Domain.Entities.Common;

/// <summary>
/// Базовая сущность с Id (UUID v7 рекомендуется)
/// </summary>
public abstract class BaseEntity : IEntity
{
    public Guid Id { get; protected set; } = Guid.CreateVersion7();
}

/// <summary>
/// Базовая сущность с аудитом
/// </summary>
public abstract class AuditableEntity : BaseEntity, IAuditableEntity, ISoftDelete
{
    public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;
    public Guid? CreatedBy { get; private set; }
    public DateTime? ModifiedAtUtc { get; private set; }
    public Guid? ModifiedBy { get; private set; }

    // Soft Delete
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAtUtc { get; private set; }
    public Guid? DeletedBy { get; private set; }

    public void SetCreatedBy(Guid userId)
    {
        CreatedBy = userId;
    }

    public void SetModified(Guid userId)
    {
        ModifiedAtUtc = DateTime.UtcNow;
        ModifiedBy = userId;
    }

    public void SoftDelete(Guid userId)
    {
        IsDeleted = true;
        DeletedAtUtc = DateTime.UtcNow;
        DeletedBy = userId;
    }

    public void Restore()
    {
        IsDeleted = false;
        DeletedAtUtc = null;
        DeletedBy = null;
    }
}
