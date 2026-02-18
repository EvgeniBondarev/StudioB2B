namespace StudioB2B.Domain.Entities.Common;

/// <summary>
/// Интерфейс для сущностей с аудитом создания
/// </summary>
public interface IAuditableEntity : IEntity
{
    DateTime CreatedAtUtc { get; }
    Guid? CreatedBy { get; }
    DateTime? ModifiedAtUtc { get; }
    Guid? ModifiedBy { get; }
}
