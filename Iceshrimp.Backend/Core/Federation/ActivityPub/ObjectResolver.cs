using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Federation.ActivityPub;

public class ObjectResolver(
	ILogger<ObjectResolver> logger,
	ActivityFetcherService fetchSvc,
	DatabaseContext db,
	FederationControlService federationCtrl
) {
	public async Task<ASObject?> ResolveObject(ASIdObject idObj) {
		if (idObj is ASObject obj) return obj;
		if (idObj.Id == null) {
			logger.LogDebug("Refusing to resolve object with null id property");
			return null;
		}

		if (await federationCtrl.ShouldBlockAsync(idObj.Id)) {
			logger.LogDebug("Instance is blocked");
			return null;
		}

		if (await db.Notes.AnyAsync(p => p.Uri == idObj.Id))
			return new ASNote { Id = idObj.Id };
		if (await db.Users.AnyAsync(p => p.Uri == idObj.Id))
			return new ASActor { Id = idObj.Id };

		try {
			var result = await fetchSvc.FetchActivityAsync(idObj.Id);
			return result.FirstOrDefault();
		}
		catch (Exception e) {
			logger.LogDebug("Failed to resolve object {id}: {error}", idObj.Id, e);
			return null;
		}
	}
}