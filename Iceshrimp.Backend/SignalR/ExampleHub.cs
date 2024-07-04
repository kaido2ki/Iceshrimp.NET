using Iceshrimp.Shared.Schemas.SignalR;
using Microsoft.AspNetCore.SignalR;

namespace Iceshrimp.Backend.SignalR;

public class ExampleHub : Hub<IExampleHubClient>, IExampleHubServer
{
	public async Task SendMessage(string user, string message)
	{
		await Clients.All.ReceiveMessage("SignalR", "ping!");
	}
}