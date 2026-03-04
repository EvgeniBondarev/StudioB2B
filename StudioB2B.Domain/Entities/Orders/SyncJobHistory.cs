namespace StudioB2B.Domain.Entities.Orders;

/// <summary>
/// История фоновых задач синхронизации заказов.
/// Не реализует IBaseEntity намеренно — чтобы аудит не писался на каждое обновление статуса.
/// </summary>
public class SyncJobHistory
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Идентификатор задачи в Hangfire.</summary>
    public string HangfireJobId { get; set; } = string.Empty;

    public SyncJobType JobType { get; set; }

    public SyncJobStatus Status { get; set; } = SyncJobStatus.Enqueued;

    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? FinishedAtUtc { get; set; }

    /// <summary>Начало периода (только для Sync).</summary>
    public DateTime? DateFrom { get; set; }

    /// <summary>Конец периода (только для Sync).</summary>
    public DateTime? DateTo { get; set; }

    /// <summary>Сериализованный OrderSyncSummary — заполняется после завершения.</summary>
    public string? ResultJson { get; set; }

    public string? ErrorMessage { get; set; }
}

