using System.Net;
using System.Security.Cryptography;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Federation.ActivityPub;
using Iceshrimp.Backend.Core.Helpers;
using Iceshrimp.Backend.Core.Middleware;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Core.Services;

public class UserService(
	IOptions<Config.InstanceSection> config,
	ILogger<UserService> logger,
	DatabaseContext db,
	ActivityFetcherService fetchSvc) {
	private (string Username, string? Host) AcctToTuple(string acct) {
		if (!acct.StartsWith("acct:")) throw new GracefulException(HttpStatusCode.BadRequest, "Invalid query");

		var split = acct[5..].Split('@');
		if (split.Length != 2) throw new GracefulException(HttpStatusCode.BadRequest, "Invalid query");

		return (split[0], split[1].ToPunycode());
	}

	public async Task<User?> GetUserFromQuery(string query) {
		if (query.StartsWith("http://") || query.StartsWith("https://"))
			if (query.StartsWith($"https://{config.Value.WebDomain}/users/")) {
				query = query[$"https://{config.Value.WebDomain}/users/".Length..];
				return await db.Users.FirstOrDefaultAsync(p => p.Id == query) ??
				       throw GracefulException.NotFound("User not found");
			}
			else {
				return await db.Users.FirstOrDefaultAsync(p => p.Uri == query);
			}

		var tuple = AcctToTuple(query);
		if (tuple.Host == config.Value.WebDomain || tuple.Host == config.Value.AccountDomain)
			tuple.Host = null;
		return await db.Users.FirstOrDefaultAsync(p => p.Username == tuple.Username && p.Host == tuple.Host);
	}

	//TODO: UpdateUser

	public async Task<User> CreateUser(string uri, string acct) {
		logger.LogDebug("Creating user {acct} with uri {uri}", acct, uri);
		var instanceActor        = await GetInstanceActor();
		var instanceActorKeypair = await db.UserKeypairs.FirstAsync(p => p.User == instanceActor);
		var actor                = await fetchSvc.FetchActor(uri, instanceActor, instanceActorKeypair);
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

		if (actor.PublicKey?.Id == null || actor.PublicKey?.PublicKey == null)
			throw new GracefulException(HttpStatusCode.UnprocessableEntity, "Actor has no valid public key");

		var publicKey = new UserPublickey {
			UserId = user.Id,
			KeyId  = actor.PublicKey.Id,
			KeyPem = actor.PublicKey.PublicKey
		};

		//TODO: add UserProfile as well

		await db.Users.AddAsync(user);
		await db.UserPublickeys.AddAsync(publicKey);
		await db.SaveChangesAsync();

		return user;
	}

	public async Task<User> CreateLocalUser(string username, string password) {
		if (username.Contains('.'))
			throw new GracefulException(HttpStatusCode.BadRequest, "Username must not contain the dot character");

		if (await db.Users.AnyAsync(p => p.Host == null && p.UsernameLower == username.ToLowerInvariant()))
			throw new GracefulException(HttpStatusCode.BadRequest, "User already exists");

		if (await db.UsedUsernames.AnyAsync(p => p.Username.ToLower() == username.ToLowerInvariant()))
			throw new GracefulException(HttpStatusCode.BadRequest, "Username was already used");

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


	public async Task<User> GetInstanceActor() {
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
			throw new GracefulException($"System user {username} already exists");

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