using StudioB2B.Domain.Constants;

namespace StudioB2B.Domain.Entities;

/// <summary>
/// Сохранённое расписание фоновой задачи для тенанта.
/// Расписание всегда хранится как cron-выражение.
/// Параметры запуска задачи хранятся в <see cref="SyncParams"/> как JSON.
/// </summary>
public class SyncJobSchedule
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public SyncJobTypeEnum JobType { get; set; }

    public bool IsEnabled { get; set; } = true;

    /// <summary>Cron-выражение (5 полей, стандарт Hangfire/Quartz).</summary>
    public string CronExpression { get; set; } = "0 9 * * *";

    /// <summary>
    /// Человекочитаемое описание расписания (заполняется сервисом при сохранении).
    /// </summary>
    public string? CronDescription { get; set; }

    /// <summary>
    /// JSON с параметрами запуска конкретной задачи.
    /// Для SyncJobType.Sync: { "DaysBack": 7 }
    /// Для SyncJobType.Update: null или {}
    /// Расширяется при появлении новых типов задач без изменения схемы.
    /// </summary>
    public string? SyncParams { get; set; }

    /// <summary>Уникальный ключ recurring job в Hangfire (null пока не создано).</summary>
    public string? HangfireRecurringJobId { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public string? CreatedByEmail { get; set; }
}
