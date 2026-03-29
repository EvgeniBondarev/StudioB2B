using Microsoft.AspNetCore.SignalR;
using StudioB2B.Infrastructure.Interfaces;
using StudioB2B.Web.Hubs;

namespace StudioB2B.Web.Services;

public class TaskBoardNotificationSender : ITaskBoardNotificationSender
{
    private readonly IHubContext<TaskBoardHub> _hub;

    public TaskBoardNotificationSender(IHubContext<TaskBoardHub> hub)
    {
        _hub = hub;
    }

    public async Task SendTaskClaimedAsync(Guid tenantId, Guid taskId, Guid userId, string userName, CancellationToken ct = default)
    {
        await _hub.Clients
            .Group(tenantId.ToString())
            .SendAsync("TaskClaimed", new { taskId, userId, userName }, ct);
    }

    public async Task SendTaskReleasedAsync(Guid tenantId, Guid taskId, CancellationToken ct = default)
    {
        await _hub.Clients
            .Group(tenantId.ToString())
            .SendAsync("TaskReleased", new { taskId }, ct);
    }

    public async Task SendTaskCompletedAsync(Guid tenantId, Guid taskId, CancellationToken ct = default)
    {
        await _hub.Clients
            .Group(tenantId.ToString())
            .SendAsync("TaskCompleted", new { taskId }, ct);
    }

    public async Task SendBoardUpdatedAsync(Guid tenantId, CancellationToken ct = default)
    {
        await _hub.Clients
            .Group(tenantId.ToString())
            .SendAsync("BoardUpdated", new { }, ct);
    }
}
