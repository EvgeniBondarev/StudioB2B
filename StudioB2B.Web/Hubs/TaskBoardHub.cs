using Microsoft.AspNetCore.SignalR;

namespace StudioB2B.Web.Hubs;

public class TaskBoardHub : Hub
{
    public async Task JoinTenantGroup(string tenantId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, tenantId);
    }
}
