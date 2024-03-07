using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Services;

public class PollService(
	DatabaseContext db,
	ActivityPub.ActivityRenderer activityRenderer,
	ActivityPub.UserRenderer userRenderer,
	ActivityPub.ActivityDeliverService deliverSvc
)
{
	public async Task RegisterPollVote(PollVote pollVote, Poll poll, Note note)
	{
		await db.Database
		        .ExecuteSqlAsync($"""UPDATE "poll" SET "votes"[{pollVote.Choice + 1}] = "votes"[{pollVote.Choice + 1}] + 1 WHERE "noteId" = {note.Id}""");
		if (poll.UserHost == null) return;

		var vote     = activityRenderer.RenderVote(pollVote, poll, note);
		var actor    = userRenderer.RenderLite(pollVote.User);
		var activity = ActivityPub.ActivityRenderer.RenderCreate(vote, actor);
		await deliverSvc.DeliverToAsync(activity, pollVote.User, note.User);
	}
}