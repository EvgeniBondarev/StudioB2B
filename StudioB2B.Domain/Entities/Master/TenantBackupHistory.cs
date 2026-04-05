namespace StudioB2B.Domain.Entities;

/// <summary>
/// Запись об одном выполненном (или выполняемом) бэкапе базы данных тенанта.
/// Хранится в мастер-БД.
/// </summary>
public class TenantBackupHistory
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TenantId { get; set; }

    /// <summary>Ключ объекта в MinIO (null пока бэкап не завершён успешно).</summary>
    public string? MinioObjectKey { get; set; }

    /// <summary>Размер загруженного файла в байтах.</summary>
    public long? SizeBytes { get; set; }

    /// <summary>Running | Completed | Failed</summary>
    public string Status { get; set; } = "Running";

    public string? ErrorMessage { get; set; }

    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedAtUtc { get; set; }
}

