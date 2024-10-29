using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;
using Iceshrimp.Backend.Core.Middleware;
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
		ASObjectBase baseObj, string? actorUri = null, int recurse = 5, bool force = false, User? user = null
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
		
		if (!Uri.TryCreate(baseObj.Id, UriKind.Absolute, out var uri) || uri.Scheme != "https"){
			logger.LogDebug("Refusing to resolve object with invalid id property");
			return null;
		}

		if (uri.Host == config.Value.WebDomain)
		{
			if (uri.AbsolutePath.StartsWith("/notes/"))
				return new ASNote { Id = baseObj.Id, VerifiedFetch = true };
			if (uri.AbsolutePath.StartsWith("/users/"))
				return new ASActor { Id = baseObj.Id };
			if (uri.AbsolutePath.StartsWith("/follows/"))
				return new ASFollow { Id = baseObj.Id };
			throw GracefulException.UnprocessableEntity($"Unable to resolve local object of unknown type: {baseObj.Id}");
		}

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
			var result      = await fetchSvc.FetchActivityAsync(baseObj.Id, user);
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

	public async IAsyncEnumerable<ASObject> IterateCollection(ASCollection? collection, User? user = null, int pageLimit = 10)
	{
		if (collection == null) yield break;

		if (collection.IsUnresolved)
			collection = await ResolveObject(collection, force: true, user: user) as ASCollection;

		if (collection == null) yield break;

		if (collection.Items != null)
			foreach (var item in collection.Items)
				yield return item;

		// some remote software (e.g. fedibird) can get in a state where page.next == page.id
		var visitedPages = new HashSet<string>();

		var page = collection.First;
		while (page != null)
		{
			if (page.IsUnresolved)
				page = await ResolveObject(page, force: true, user: user) as ASCollectionPage;

			if (page == null) break;

			if (page.Items != null)
				foreach (var item in page.Items)
					yield return item;

			if (page.Next?.Id != null)
				if (!visitedPages.Add(page.Next.Id))
					break;

			// we only limit based on pages here. the consumer of this iterator may
			// additionally limit per-item via System.Linq.Async Take()
			if (--pageLimit <= 0)
				break;

			page = page.Next;
		}
	}
}