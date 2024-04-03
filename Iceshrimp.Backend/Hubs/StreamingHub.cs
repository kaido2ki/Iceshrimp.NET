using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Iceshrimp.Backend.Hubs;

[Authorize(Policy = "HubAuthorization")]
public class StreamingHub : Hub
{
	public async Task SendMessage(string user, string message)
	{
		await Clients.All.SendAsync("ReceiveMessage", "SignalR", "ping!");
	}
}