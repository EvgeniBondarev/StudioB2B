using Microsoft.AspNetCore.SignalR;

namespace StudioB2B.Web.Hubs;

/// <summary>
/// SignalR Hub для push-уведомлений от Ozon.
/// Клиент подключается и подписывается на группу своего тенанта.
/// </summary>
public class OzonPushHub : Hub
{
    public async Task JoinTenantGroup(string tenantId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, tenantId);
    }
}

