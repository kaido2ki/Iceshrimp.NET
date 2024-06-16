using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Core.Federation.ActivityPub;

public class ObjectResolver(
	ILogger<ObjectResolver> logger,
	ActivityFetcherService fetchSvc,
	DatabaseContext db,
	FederationControlService federationCtrl,
	IOptions<Config.InstanceSection> config
)
{
	public async Task<ASObject?> ResolveObject(
		ASObjectBase baseObj, string? actorUri = null, int recurse = 5, bool force = false
	)
	{
		logger.LogDebug("Resolving object: {id}", baseObj.Id ?? "<anonymous>");

		if (baseObj is ASActivity { Object.IsUnresolved: true } activity && recurse > 0)
		{
			activity.Object = await ResolveObject(activity.Object, actorUri, --recurse, force);
			return await ResolveObject(activity, actorUri, recurse);
		}

		if (baseObj is ASObject { IsUnresolved: false } obj && !force)
		{
			if (actorUri == null ||
			    baseObj is not ASNote { AttributedTo.Count: > 0 } note ||
			    note.AttributedTo.Any(p => p.Id != actorUri))
			{
				return obj;
			}

			note.VerifiedFetch = true;
			return note;
		}

		if (baseObj.Id == null)
		{
			logger.LogDebug("Refusing to resolve object with null id property");
			return null;
		}

		if (baseObj.Id.StartsWith($"https://{config.Value.WebDomain}/notes/"))
			return new ASNote { Id = baseObj.Id, VerifiedFetch = true };
		if (baseObj.Id.StartsWith($"https://{config.Value.WebDomain}/users/"))
			return new ASActor { Id = baseObj.Id };
		if (baseObj.Id.StartsWith($"https://{config.Value.WebDomain}/follows/"))
			return new ASFollow { Id = baseObj.Id };

		if (await federationCtrl.ShouldBlockAsync(baseObj.Id))
		{
			logger.LogDebug("Instance is blocked ({uri})", baseObj.Id);
			return null;
		}

		if (await db.Notes.AnyAsync(p => p.Uri == baseObj.Id))
			return new ASNote { Id = baseObj.Id, VerifiedFetch = true };
		if (await db.Users.AnyAsync(p => p.Uri == baseObj.Id))
			return new ASActor { Id = baseObj.Id };

		try
		{
			var result      = await fetchSvc.FetchActivityAsync(baseObj.Id);
			var resolvedObj = result.FirstOrDefault();
			if (resolvedObj is not ASNote note) return resolvedObj;
			note.VerifiedFetch = true;
			return note;
		}
		catch (Exception e)
		{
			logger.LogDebug("Failed to resolve object {id}: {error}", baseObj.Id, e);
			return null;
		}
	}
}