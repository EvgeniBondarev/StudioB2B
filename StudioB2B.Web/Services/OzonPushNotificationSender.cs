using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using StudioB2B.Infrastructure.Interfaces;
using StudioB2B.Shared;
using StudioB2B.Web.Hubs;

namespace StudioB2B.Web.Services;

public class OzonPushNotificationSender : IOzonPushNotificationSender
{
    private readonly IHubContext<OzonPushHub> _hub;
    private readonly ILogger<OzonPushNotificationSender> _logger;

    public OzonPushNotificationSender(IHubContext<OzonPushHub> hub, ILogger<OzonPushNotificationSender> logger)
    {
        _hub = hub;
        _logger = logger;
    }

    public async Task SendPushAsync(string tenantId, OzonPushNotificationDto notification, CancellationToken ct = default)
    {
        try
        {
            await _hub.Clients
                .Group(tenantId)
                .SendAsync("OzonPush", notification, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "OzonPush: failed to broadcast {MessageType} to tenant {TenantId} via SignalR.",
                notification.MessageType, tenantId);
        }
    }
}
