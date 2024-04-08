using Iceshrimp.Shared.Schemas;

namespace Iceshrimp.Shared.HubSchemas;

public interface IStreamingHubServer
{
	public Task SendMessage(string user, string message);
	public Task Subscribe(StreamingTimeline timeline);
	public Task Unsubscribe(StreamingTimeline timeline);
}

public interface IStreamingHubClient
{
	public Task ReceiveMessage(string user, string message);
	public Task Notification(NotificationResponse notification);
	public Task NotePublished(List<StreamingTimeline> timelines, NoteResponse note);
	public Task NoteUpdated(List<StreamingTimeline> timelines, NoteResponse note);
}

public enum StreamingTimeline
{
	Home,
	Local,
	Federated
}