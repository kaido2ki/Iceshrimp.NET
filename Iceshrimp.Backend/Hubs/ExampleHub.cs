using Microsoft.AspNetCore.SignalR;
namespace Iceshrimp.Backend.Hubs;

public class ExampleHub : Hub
{
	public async Task SendMessage(string user, string message)
	{
		await Clients.All.SendAsync("ReceiveMessage", user, message);
	}
}