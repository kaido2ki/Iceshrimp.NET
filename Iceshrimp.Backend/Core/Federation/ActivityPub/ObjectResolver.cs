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
	public async Task<ASObject?> ResolveObject(ASObjectBase baseObj) {
		if (baseObj is ASObject obj) return obj;
		if (baseObj.Id == null) {
			logger.LogDebug("Refusing to resolve object with null id property");
			return null;
		}

		if (await federationCtrl.ShouldBlockAsync(baseObj.Id)) {
			logger.LogDebug("Instance is blocked");
			return null;
		}

		if (await db.Notes.AnyAsync(p => p.Uri == baseObj.Id))
			return new ASNote { Id = baseObj.Id };
		if (await db.Users.AnyAsync(p => p.Uri == baseObj.Id))
			return new ASActor { Id = baseObj.Id };

		try {
			var result = await fetchSvc.FetchActivityAsync(baseObj.Id);
			return result.FirstOrDefault();
		}
		catch (Exception e) {
			logger.LogDebug("Failed to resolve object {id}: {error}", baseObj.Id, e);
			return null;
		}
	}
}