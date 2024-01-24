using System.Net;
using System.Security.Cryptography;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Federation.ActivityPub;
using Iceshrimp.Backend.Core.Helpers;
using Iceshrimp.Backend.Core.Middleware;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Services;

public class UserService(ILogger<UserService> logger, DatabaseContext db, ActivityPubService apSvc) {
	private (string Username, string Host) AcctToTuple(string acct) {
		if (!acct.StartsWith("acct:")) throw new CustomException(HttpStatusCode.BadRequest, "Invalid query");

		var split = acct[5..].Split('@');
		if (split.Length != 2) throw new CustomException(HttpStatusCode.BadRequest, "Invalid query");

		return (split[0], split[1]);
	}

	public Task<User?> GetUserFromQuery(string query) {
		if (query.StartsWith("http://") || query.StartsWith("https://"))
			return db.Users.FirstOrDefaultAsync(p => p.Uri == query);

		var tuple = AcctToTuple(query);
		return db.Users.FirstOrDefaultAsync(p => p.Username == tuple.Username && p.Host == tuple.Host);
	}

	//TODO: UpdateUser

	public async Task<User> CreateUser(string uri, string acct) {
		logger.LogDebug("Creating user {acct} with uri {uri}", acct, uri);
		var instanceActor        = await GetInstanceActor();
		var instanceActorKeypair = await db.UserKeypairs.FirstAsync(p => p.UserId == instanceActor.Id);
		var actor                = await apSvc.FetchActor(uri, instanceActor, instanceActorKeypair);
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
			//TODO: FollowersCount
			//TODO: FollowingCount
			Emojis = [], //FIXME
			Tags   = []  //FIXME
		};

		//TODO: add UserProfile as well

		await db.Users.AddAsync(user);
		await db.SaveChangesAsync();

		return user;
	}

	public async Task<User> CreateLocalUser(string username, string password) {
		if (await db.Users.AnyAsync(p => p.Host == null && p.UsernameLower == username.ToLowerInvariant()))
			throw new CustomException(HttpStatusCode.BadRequest, "User already exists");

		if (await db.UsedUsernames.AnyAsync(p => p.Username.ToLower() == username.ToLowerInvariant()))
			throw new CustomException(HttpStatusCode.BadRequest, "Username was already used");

		var keypair = RSA.Create(4096);
		var user = new User {
			Id            = IdHelpers.GenerateSlowflakeId(),
			CreatedAt     = DateTime.UtcNow,
			Username      = username,
			UsernameLower = username.ToLowerInvariant(),
			Host          = null
		};

		var userKeypair = new UserKeypair {
			UserId     = user.Id,
			PrivateKey = keypair.ExportPkcs8PrivateKeyPem(),
			PublicKey  = keypair.ExportSubjectPublicKeyInfoPem()
		};

		var userProfile = new UserProfile {
			UserId   = user.Id,
			Password = AuthHelpers.HashPassword(password)
		};

		var usedUsername = new UsedUsername {
			CreatedAt = DateTime.UtcNow,
			Username  = username.ToLowerInvariant()
		};

		await db.AddRangeAsync(user, userKeypair, userProfile, usedUsername);
		await db.SaveChangesAsync();

		return user;
	}


	private async Task<User> GetInstanceActor() {
		return await GetOrCreateSystemUser("instance.actor");
	}

	public async Task<User> GetRelayActor() {
		return await GetOrCreateSystemUser("relay.actor");
	}

	//TODO: cache in redis
	private async Task<User> GetOrCreateSystemUser(string username) {
		return await db.Users.FirstOrDefaultAsync(p => p.UsernameLower == username && p.Host == null) ??
		       await CreateSystemUser(username);
	}

	private async Task<User> CreateSystemUser(string username) {
		if (await db.Users.AnyAsync(p => p.UsernameLower == username.ToLowerInvariant() && p.Host == null))
			throw new CustomException($"System user {username} already exists");

		var keypair = RSA.Create(4096);
		var user = new User {
			Id            = IdHelpers.GenerateSlowflakeId(),
			CreatedAt     = DateTime.UtcNow,
			Username      = username,
			UsernameLower = username.ToLowerInvariant(),
			Host          = null,
			IsAdmin       = false,
			IsLocked      = true,
			IsExplorable  = false,
			IsBot         = true
		};

		var userKeypair = new UserKeypair {
			UserId     = user.Id,
			PrivateKey = keypair.ExportPkcs8PrivateKeyPem(),
			PublicKey  = keypair.ExportSubjectPublicKeyInfoPem()
		};

		var userProfile = new UserProfile {
			UserId             = user.Id,
			AutoAcceptFollowed = false,
			Password           = null
		};

		var usedUsername = new UsedUsername {
			CreatedAt = DateTime.UtcNow,
			Username  = username.ToLowerInvariant()
		};

		await db.AddRangeAsync(user, userKeypair, userProfile, usedUsername);
		await db.SaveChangesAsync();

		return user;
	}
}