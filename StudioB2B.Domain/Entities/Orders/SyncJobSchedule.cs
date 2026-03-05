namespace StudioB2B.Domain.Entities.Orders;

/// <summary>
/// Сохранённое расписание фоновой задачи для тенанта.
/// </summary>
public class SyncJobSchedule
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public SyncJobType JobType { get; set; }

    public ScheduleType ScheduleType { get; set; }

    public bool IsEnabled { get; set; } = true;

    // ── Параметры расписания ──────────────────────────────────────────

    /// <summary>Для EveryNMinutes: интервал в минутах.</summary>
    public int? IntervalMinutes { get; set; }

    /// <summary>Для EveryNHours: интервал в часах.</summary>
    public int? IntervalHours { get; set; }

    /// <summary>Для EveryNDays: интервал в днях.</summary>
    public int? IntervalDays { get; set; }

    /// <summary>Для DailyAt / WeeklyAt / MonthlyAt / EveryNDays: время суток.</summary>
    public TimeSpan? TimeOfDay { get; set; }

    /// <summary>
    /// Для WeeklyAt: дни недели через запятую (0=вс,1=пн,...,6=сб).
    /// Хранится как строка, например "1,3,5".
    /// </summary>
    public string? DaysOfWeek { get; set; }

    /// <summary>Для MonthlyAt: день месяца (1–28).</summary>
    public int? DayOfMonth { get; set; }

    /// <summary>Для CustomCron: произвольное cron-выражение.</summary>
    public string? CronExpression { get; set; }

    // ── Параметры задачи Sync ─────────────────────────────────────────

    /// <summary>
    /// Для JobType = Sync: сколько дней назад брать начало периода
    /// (период = [now - SyncDaysBack, now]).
    /// </summary>
    public int SyncDaysBack { get; set; } = 7;

    // ── Hangfire ──────────────────────────────────────────────────────

    /// <summary>Уникальный ключ recurring job в Hangfire (null пока не создано).</summary>
    public string? HangfireRecurringJobId { get; set; }

    // ── Аудит ─────────────────────────────────────────────────────────

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public string? CreatedByEmail { get; set; }
}

