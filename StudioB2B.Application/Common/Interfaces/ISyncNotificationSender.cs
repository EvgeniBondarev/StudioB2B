namespace StudioB2B.Application.Common.Interfaces;

/// <summary>
/// Абстракция для отправки уведомлений о задачах синхронизации.
/// Реализуется в Web-слое через IHubContext&lt;SyncNotificationHub&gt;.
/// </summary>
public interface ISyncNotificationSender
{
    /// <summary>Уведомляет клиентов о старте новой задачи (запущена по расписанию или вручную).</summary>
    Task SendJobStartedAsync(
        Guid tenantId,
        Guid historyId,
        string jobType,
        CancellationToken ct = default);

    Task SendJobCompletedAsync(
        Guid tenantId,
        Guid historyId,
        string status,
        string jobType,
        CancellationToken ct = default);
}

