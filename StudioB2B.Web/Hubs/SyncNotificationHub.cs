using Microsoft.AspNetCore.SignalR;

namespace StudioB2B.Web.Hubs;

/// <summary>
/// SignalR Hub для push-уведомлений о завершении фоновых задач синхронизации.
/// Клиент подключается и подписывается на группу своего тенанта.
/// </summary>
public class SyncNotificationHub : Hub
{
    /// <summary>
    /// Клиент вызывает этот метод после подключения, чтобы получать уведомления своего тенанта.
    /// </summary>
    public async Task JoinTenantGroup(string tenantId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, tenantId);
    }
}

