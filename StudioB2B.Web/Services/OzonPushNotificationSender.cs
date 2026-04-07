using Microsoft.AspNetCore.SignalR;
using StudioB2B.Infrastructure.Interfaces;
using StudioB2B.Shared;
using StudioB2B.Web.Hubs;

namespace StudioB2B.Web.Services;

public class OzonPushNotificationSender : IOzonPushNotificationSender
{
    private readonly IHubContext<OzonPushHub> _hub;

    public OzonPushNotificationSender(IHubContext<OzonPushHub> hub)
    {
        _hub = hub;
    }

    public async Task SendPushAsync(string tenantId, OzonPushNotificationDto notification, CancellationToken ct = default)
    {
        await _hub.Clients
            .Group(tenantId)
            .SendAsync("OzonPush", notification, ct);
    }
}

