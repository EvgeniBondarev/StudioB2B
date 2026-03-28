namespace StudioB2B.Infrastructure.Interfaces;

public interface ITaskBoardNotificationSender
{
    Task SendTaskClaimedAsync(Guid tenantId, Guid taskId, Guid userId, string userName, CancellationToken ct = default);
    Task SendTaskReleasedAsync(Guid tenantId, Guid taskId, CancellationToken ct = default);
    Task SendTaskCompletedAsync(Guid tenantId, Guid taskId, CancellationToken ct = default);
    Task SendBoardUpdatedAsync(Guid tenantId, CancellationToken ct = default);
}
