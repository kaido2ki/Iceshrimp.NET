using Iceshrimp.Backend.Core.Database.Tables;

namespace Iceshrimp.Backend.Core.Services;

public class PollService(
	ActivityPub.ActivityRenderer activityRenderer,
	ActivityPub.UserRenderer userRenderer,
	ActivityPub.ActivityDeliverService deliverSvc
)
{
	public async Task RegisterPollVote(PollVote pollVote, Poll poll, Note note)
	{
		if (poll.UserHost == null) return;

		var vote     = activityRenderer.RenderVote(pollVote, poll, note);
		var actor    = userRenderer.RenderLite(pollVote.User);
		var activity = ActivityPub.ActivityRenderer.RenderCreate(vote, actor);
		await deliverSvc.DeliverToAsync(activity, pollVote.User, note.User);
	}
}