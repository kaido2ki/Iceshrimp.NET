using Iceshrimp.Shared.HubSchemas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Iceshrimp.Backend.Hubs;

[Authorize(Policy = "HubAuthorization")]
public class StreamingHub : Hub<IStreamingHubClient>, IStreamingHubServer
{
	public async Task SendMessage(string user, string message)
	{
		await Clients.All.ReceiveMessage("SignalR", "ping!");
	}

	public override async Task OnConnectedAsync()
	{
		await base.OnConnectedAsync();
		var userId = Context.UserIdentifier ?? throw new Exception("UserIdentifier must not be null at this stage");
		await Clients.User(userId).ReceiveMessage("SignalR", "Device connected");
	}
}