using System.Text.RegularExpressions;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Federation.ActivityPub;
using Iceshrimp.Backend.Core.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Services;

public class UserService(ILogger<UserService> logger, DatabaseContext db, HttpClient client, ActivityPubService apSvc) {
	private static (string Username, string Host) AcctToTuple(string acct) {
		if (!acct.StartsWith("acct:")) throw new Exception("Invalid query");

		var split = acct[5..].Split('@');
		if (split.Length != 2) throw new Exception("Invalid query");

		return (split[0], split[1]);
	}

	public Task<User?> GetUserFromQuery(string query) {
		if (query.StartsWith("http://") || query.StartsWith("https://")) {
			return db.Users.FirstOrDefaultAsync(p => p.Uri == query);
		}

		var tuple = AcctToTuple(query);
		return db.Users.FirstOrDefaultAsync(p => p.Username == tuple.Username && p.Host == tuple.Host);
	}

	public async Task<User> CreateUser(string uri, string acct) {
		logger.LogDebug("Creating user {acct} with uri {uri}", acct, uri);
		var actor = await apSvc.FetchActor(uri);
		logger.LogDebug("Got actor: {url}", actor.Url);

		actor.Normalize(uri, acct);

		var user = new User {
			Id            = IdHelpers.GenerateSlowflakeId(),
			CreatedAt     = DateTime.UtcNow,
			LastFetchedAt = DateTime.UtcNow,
			Name          = actor.DisplayName,
			IsLocked      = actor.IsLocked ?? false,
			IsBot         = actor.IsBot,
			Username      = actor.Username!,
			UsernameLower = actor.Username!.ToLowerInvariant(),
			Host          = AcctToTuple(acct).Host,
			MovedToUri    = actor.MovedTo?.Link,
			AlsoKnownAs   = actor.AlsoKnownAs?.Link,
			IsExplorable  = actor.IsDiscoverable ?? false,
			Inbox         = actor.Inbox?.Link,
			SharedInbox   = actor.SharedInbox?.Link,
			FollowersUri  = actor.Followers?.Id,
			Uri           = actor.Id,
			IsCat         = actor.IsCat ?? false,
			Featured      = actor.Featured?.Link,
			//FollowersCount
			//FollowingCount
			Emojis        = [], //FIXME
			Tags          = [], //FIXME
		};

		//TODO: add UserProfile as well

		await db.Users.AddAsync(user);
		await db.SaveChangesAsync();

		return user;
	}
}