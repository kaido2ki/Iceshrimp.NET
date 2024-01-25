using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Federation.ActivityPub;
using Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;

namespace Iceshrimp.Backend.Core.Services;

public class NoteService(ILogger<NoteService> logger, DatabaseContext db, UserResolver userResolver) {
	public async Task CreateNote(ASNote note, ASActor actor) {
		var user = await userResolver.Resolve(actor.Id);
		logger.LogDebug("Resolved user to {user}", user.Id);
		//TODO: insert into database
		//TODO: resolve anything related to the note as well (reply thread, attachments, emoji, etc)
	}
}