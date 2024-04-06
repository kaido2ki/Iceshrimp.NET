using Iceshrimp.Shared.HubSchemas;
using Microsoft.AspNetCore.SignalR;
namespace Iceshrimp.Backend.Hubs;

public class ExampleHub : Hub<IExampleHubClient>, IExampleHubServer
{
	public async Task SendMessage(string user, string message)
	{
		await Clients.All.ReceiveMessage("SignalR", "ping!");
	}
}