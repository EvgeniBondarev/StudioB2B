namespace StudioB2B.Application.Common.Interfaces;

/// <summary>
/// Абстракция для отправки уведомлений о завершении задачи синхронизации.
/// Реализуется в Web-слое через IHubContext&lt;SyncNotificationHub&gt;.
/// </summary>
public interface ISyncNotificationSender
{
    Task SendJobCompletedAsync(
        Guid tenantId,
        Guid historyId,
        string status,
        string jobType,
        CancellationToken ct = default);
}

