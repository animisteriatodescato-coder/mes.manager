using Microsoft.AspNetCore.SignalR;

namespace MESManager.Web.Hubs;

public class RealtimeHub : Hub
{
    public async Task SendUpdate(string message)
    {
        await Clients.All.SendAsync("ReceiveUpdate", message);
    }
}
