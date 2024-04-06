namespace Iceshrimp.Shared.HubSchemas;

public interface IStreamingHubServer
{
	public Task SendMessage(string user, string message);
}

public interface IStreamingHubClient
{
	public Task ReceiveMessage(string user, string message);
}