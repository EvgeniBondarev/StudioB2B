namespace StudioB2B.Domain.Entities.Common;

/// <summary>
/// Интерфейс для soft delete
/// </summary>
public interface ISoftDelete
{
    bool IsDeleted { get; }
    DateTime? DeletedAtUtc { get; }
    Guid? DeletedBy { get; }
}
