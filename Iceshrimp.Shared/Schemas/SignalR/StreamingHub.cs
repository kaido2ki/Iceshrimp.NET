using Iceshrimp.Shared.Schemas.Web;

namespace Iceshrimp.Shared.Schemas.SignalR;

public interface IStreamingHubServer
{
	public Task Subscribe(StreamingTimeline timeline);
	public Task Unsubscribe(StreamingTimeline timeline);
}

public interface IStreamingHubClient
{
	public Task Notification(NotificationResponse notification);
	public Task NotePublished(List<StreamingTimeline> timelines, NoteResponse note);
	public Task NoteUpdated(NoteResponse note);
	public Task NoteDeleted(string noteId);

	public Task FilterAdded(FilterResponse filter);
	public Task FilterUpdated(FilterResponse filter);
	public Task FilterRemoved(long filterId);
}

public enum StreamingTimeline
{
	Home,
	Local,
	Federated
}