namespace StudioB2B.Domain.Entities;

/// <summary>
/// Запись об одной выполненной (или выполняемой) операции восстановления базы данных тенанта.
/// Хранится в мастер-БД.
/// </summary>
public class TenantRestoreHistory
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TenantId { get; set; }

    /// <summary>Ключ объекта в MinIO, из которого выполнялось восстановление.</summary>
    public string SourceObjectKey { get; set; } = "";

    /// <summary>SavedBackup | Upload</summary>
    public string SourceType { get; set; } = "SavedBackup";

    /// <summary>Running | Completed | Failed</summary>
    public string Status { get; set; } = "Running";

    public string? ErrorMessage { get; set; }

    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedAtUtc { get; set; }
}

