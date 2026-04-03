namespace StudioB2B.Domain.Entities;

/// <summary>
/// Расписание автоматических бэкапов базы данных тенанта.
/// Хранится в мастер-БД; управляется только Admin-пользователями.
/// </summary>
public class TenantBackupSchedule
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TenantId { get; set; }

    public TenantEntity? Tenant { get; set; }

    public bool IsEnabled { get; set; } = true;

    /// <summary>Cron-выражение (5 полей, стандарт Hangfire). Например: "0 2 * * *" — каждый день в 02:00 UTC.</summary>
    public string CronExpression { get; set; } = "0 2 * * *";

    /// <summary>Сколько дней хранить бэкапы в MinIO.</summary>
    public int RetentionDays { get; set; } = 30;

    /// <summary>ID recurring job в Hangfire мастер-сервера (null до первого сохранения).</summary>
    public string? HangfireJobId { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

