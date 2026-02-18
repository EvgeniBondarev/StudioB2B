namespace StudioB2B.Domain.Entities.Common;

/// <summary>
/// Базовый интерфейс для всех сущностей
/// </summary>
public interface IEntity
{
    Guid Id { get; }
}
