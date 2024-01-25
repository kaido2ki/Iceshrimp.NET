using Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;
using Iceshrimp.Backend.Core.Services;

namespace Iceshrimp.Backend.Core.Federation.ActivityPub;

public class APService(ILogger<APService> logger, NoteService noteSvc) {
	public async Task PerformActivity(ASActivity activity, string? inboxUserId) {
		logger.LogDebug("Processing activity: {activity}", activity.Id);
		if (activity.Actor == null) throw new Exception("Cannot perform activity as actor 'null'");

		//TODO: validate inboxUserId

		if (activity.Object is ASNote note) await noteSvc.CreateNote(note, activity.Actor);
		else throw new NotImplementedException();
	}
}