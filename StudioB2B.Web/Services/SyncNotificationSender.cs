using Microsoft.AspNetCore.SignalR;
using StudioB2B.Application.Common.Interfaces;
using StudioB2B.Web.Hubs;

namespace StudioB2B.Web.Services;

public class SyncNotificationSender : ISyncNotificationSender
{
    private readonly IHubContext<SyncNotificationHub> _hub;

    public SyncNotificationSender(IHubContext<SyncNotificationHub> hub)
    {
        _hub = hub;
    }

    public async Task SendJobStartedAsync(
        Guid tenantId,
        Guid historyId,
        string jobType,
        CancellationToken ct = default)
    {
        await _hub.Clients
            .Group(tenantId.ToString())
            .SendAsync("JobStarted", new { historyId, jobType }, ct);
    }

    public async Task SendJobCompletedAsync(
        Guid tenantId,
        Guid historyId,
        string status,
        string jobType,
        CancellationToken ct = default)
    {
        await _hub.Clients
            .Group(tenantId.ToString())
            .SendAsync("JobCompleted", new { historyId, status, jobType }, ct);
    }
}
