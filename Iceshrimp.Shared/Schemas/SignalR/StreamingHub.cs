using Iceshrimp.Shared.Schemas.Web;

namespace Iceshrimp.Shared.Schemas.SignalR;

public interface IStreamingHubServer
{
	public Task SubscribeAsync(StreamingTimeline timeline);
	public Task UnsubscribeAsync(StreamingTimeline timeline);
}

public interface IStreamingHubClient
{
	public Task NotificationAsync(NotificationResponse notification);
	public Task NotePublishedAsync(List<StreamingTimeline> timelines, NoteResponse note);
	public Task NoteUpdatedAsync(NoteResponse note);
	public Task NoteDeletedAsync(string noteId);

	public Task FilterAddedAsync(FilterResponse filter);
	public Task FilterUpdatedAsync(FilterResponse filter);
	public Task FilterRemovedAsync(long filterId);
}

public enum StreamingTimeline
{
	Home,
	Local,
	Federated
}