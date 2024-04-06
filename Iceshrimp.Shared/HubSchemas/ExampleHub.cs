namespace Iceshrimp.Shared.HubSchemas;

public interface IExampleHubServer
{
	public Task SendMessage(string user, string message);
}

public interface IExampleHubClient
{
	public Task ReceiveMessage(string user, string message);
}