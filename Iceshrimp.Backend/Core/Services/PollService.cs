using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Services;

public class PollService(
	DatabaseContext db,
	ActivityPub.ActivityRenderer activityRenderer,
	ActivityPub.UserRenderer userRenderer,
	ActivityPub.ActivityDeliverService deliverSvc
) : IScopedService
{
	public async Task RegisterPollVoteAsync(PollVote pollVote, Poll poll, Note note, bool updateVotersCount = true)
	{
		await db.Database
		        .ExecuteSqlAsync($"""UPDATE "poll" SET "votes"[{pollVote.Choice + 1}] = "votes"[{pollVote.Choice + 1}] + 1 WHERE "noteId" = {note.Id}""");

		if (poll.UserHost == null)
		{
			if (!updateVotersCount) return;
			await db.Database
			        .ExecuteSqlAsync($"""UPDATE "poll" SET "votersCount" = (SELECT COUNT(*) FROM (SELECT DISTINCT "userId" FROM "poll_vote" WHERE "noteId" = {poll.NoteId}) AS sq) WHERE "noteId" = {poll.NoteId};""");
			return;
		}

		if (updateVotersCount)
		{
			if (poll.Multiple)
				await db.Database
				        .ExecuteSqlAsync($"""UPDATE "poll" SET "votersCount" = GREATEST("votersCount", (SELECT MAX("total") FROM UNNEST("votes") AS "total")::integer, (SELECT COUNT(*) FROM (SELECT DISTINCT "userId" FROM "poll_vote" WHERE "noteId" = "poll"."noteId") AS sq)::integer) WHERE "noteId" = {poll.NoteId};""");
			else
				await db.Database
				        .ExecuteSqlAsync($"""UPDATE "poll" SET "votersCount" = GREATEST("votersCount", (SELECT SUM("total") FROM UNNEST("votes") AS "total")::integer, (SELECT COUNT(*) FROM (SELECT DISTINCT "userId" FROM "poll_vote" WHERE "noteId" = "poll"."noteId") AS sq)::integer) WHERE "noteId" = {poll.NoteId};""");
		}

		var vote     = activityRenderer.RenderVote(pollVote, poll, note);
		var actor    = userRenderer.RenderLite(pollVote.User);
		var activity = ActivityPub.ActivityRenderer.RenderCreate(vote, actor);
		await deliverSvc.DeliverToAsync(activity, pollVote.User, note.User);
	}
}