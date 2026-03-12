namespace StudioB2B.Domain.Entities;

/// <summary>
/// Запись об изменении одного скалярного поля любой сущности.
/// </summary>
public class FieldAuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Имя CLR-типа сущности (например «Order»).</summary>
    public string EntityName { get; set; } = string.Empty;

    /// <summary>Первичный ключ изменённой записи (строковое представление).</summary>
    public string EntityId { get; set; } = string.Empty;

    /// <summary>Имя изменённого свойства.</summary>
    public string FieldName { get; set; } = string.Empty;

    /// <summary>Предыдущее значение в виде JSON-строки (null — для новых записей).</summary>
    public string? OldValue { get; set; }

    /// <summary>Новое значение в виде JSON-строки (null — при удалении).</summary>
    public string? NewValue { get; set; }

    /// <summary>Id пользователя, инициировавшего изменение.</summary>
    public Guid? ChangedByUserId { get; set; }

    /// <summary>Email пользователя, инициировавшего изменение.</summary>
    public string? ChangedByUserName { get; set; }

    /// <summary>Дата и время изменения (UTC).</summary>
    public DateTime ChangedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>Тип операции: «Added», «Modified», «Deleted».</summary>
    public string ChangeType { get; set; } = string.Empty;
}

