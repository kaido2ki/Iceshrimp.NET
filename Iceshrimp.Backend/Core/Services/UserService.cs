using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Security.Cryptography;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Federation.ActivityPub;
using Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;
using Iceshrimp.Backend.Core.Helpers;
using Iceshrimp.Backend.Core.Helpers.LibMfm.Conversion;
using Iceshrimp.Backend.Core.Middleware;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Core.Services;

public class UserService(
	[SuppressMessage("ReSharper", "SuggestBaseTypeForParameterInConstructor")]
	IOptionsSnapshot<Config.SecuritySection> security,
	IOptions<Config.InstanceSection> instance,
	ILogger<UserService> logger,
	DatabaseContext db,
	ActivityFetcherService fetchSvc,
	DriveService driveSvc
) {
	private (string Username, string? Host) AcctToTuple(string acct) {
		if (!acct.StartsWith("acct:")) throw new GracefulException(HttpStatusCode.BadRequest, "Invalid query");

		var split = acct[5..].Split('@');
		if (split.Length != 2) throw new GracefulException(HttpStatusCode.BadRequest, "Invalid query");

		return (split[0], split[1].ToPunycode());
	}

	public async Task<User?> GetUserFromQueryAsync(string query) {
		if (query.StartsWith("http://") || query.StartsWith("https://"))
			if (query.StartsWith($"https://{instance.Value.WebDomain}/users/")) {
				query = query[$"https://{instance.Value.WebDomain}/users/".Length..];
				return await db.Users.FirstOrDefaultAsync(p => p.Id == query) ??
				       throw GracefulException.NotFound("User not found");
			}
			else {
				return await db.Users.FirstOrDefaultAsync(p => p.Uri == query);
			}

		var tuple = AcctToTuple(query);
		if (tuple.Host == instance.Value.WebDomain || tuple.Host == instance.Value.AccountDomain)
			tuple.Host = null;
		return await db.Users.FirstOrDefaultAsync(p => p.Username == tuple.Username && p.Host == tuple.Host);
	}

	public async Task<User> CreateUserAsync(string uri, string acct) {
		logger.LogDebug("Creating user {acct} with uri {uri}", acct, uri);
		var actor = await fetchSvc.FetchActorAsync(uri);
		logger.LogDebug("Got actor: {url}", actor.Url);

		actor.Normalize(uri, acct);

		if (actor.PublicKey?.Id == null || actor.PublicKey?.PublicKey == null)
			throw new GracefulException(HttpStatusCode.UnprocessableEntity, "Actor has no valid public key");

		var user = new User {
			Id            = IdHelpers.GenerateSlowflakeId(),
			CreatedAt     = DateTime.UtcNow,
			LastFetchedAt = DateTime.UtcNow,
			DisplayName   = actor.DisplayName,
			IsLocked      = actor.IsLocked ?? false,
			IsBot         = actor.IsBot,
			Username      = actor.Username!,
			UsernameLower = actor.Username!.ToLowerInvariant(),
			Host          = AcctToTuple(acct).Host,
			MovedToUri    = actor.MovedTo?.Link,
			AlsoKnownAs   = actor.AlsoKnownAs?.Link,
			IsExplorable  = actor.IsDiscoverable ?? false,
			Inbox         = actor.Inbox?.Link,
			SharedInbox   = actor.SharedInbox?.Link ?? actor.Endpoints?.SharedInbox?.Id,
			FollowersUri  = actor.Followers?.Id,
			Uri           = actor.Id,
			IsCat         = actor.IsCat ?? false,
			Featured      = actor.Featured?.Link,
			//TODO: FollowersCount
			//TODO: FollowingCount
			Emojis = [], //FIXME
			Tags   = []  //FIXME
		};

		await ResolveAvatarAndBanner(user, actor);

		var profile = new UserProfile {
			User        = user,
			Description = actor.MkSummary ?? await MfmConverter.FromHtmlAsync(actor.Summary),
			//Birthday = TODO,
			//Location = TODO,
			//Fields = TODO,
			UserHost = user.Host,
			Url      = actor.Url?.Link
		};

		var publicKey = new UserPublickey {
			UserId = user.Id,
			KeyId  = actor.PublicKey.Id,
			KeyPem = actor.PublicKey.PublicKey
		};

		await db.AddRangeAsync(user, profile, publicKey);
		await db.SaveChangesAsync();

		return user;
	}

	public async Task<User> UpdateUserAsync(User user) {
		if (!user.NeedsUpdate) return user;

		// Prevent multiple update jobs from running concurrently
		db.Update(user);
		await db.Users.Where(u => u.Id == user.Id)
		        .ExecuteUpdateAsync(p => p.SetProperty(u => u.LastFetchedAt, DateTime.UtcNow));

		var uri = user.Uri ?? throw new Exception("Encountered remote user without a Uri");
		logger.LogDebug("Updating user with uri {uri}", uri);

		var actor = await fetchSvc.FetchActorAsync(user.Uri);
		actor.Normalize(uri, user.Acct);

		user.UserProfile ??= await db.UserProfiles.FirstOrDefaultAsync(p => p.User == user);
		user.UserProfile ??= new UserProfile { User = user };

		user.LastFetchedAt = DateTime.UtcNow; // If we don't do this we'll overwrite the value with the previous one
		user.Inbox         = actor.Inbox?.Link;
		user.SharedInbox   = actor.SharedInbox?.Link ?? actor.Endpoints?.SharedInbox?.Id;
		user.DisplayName   = actor.DisplayName;
		user.IsLocked      = actor.IsLocked ?? false;
		user.IsBot         = actor.IsBot;
		user.MovedToUri    = actor.MovedTo?.Link;
		user.AlsoKnownAs   = actor.AlsoKnownAs?.Link;
		user.IsExplorable  = actor.IsDiscoverable ?? false;
		user.FollowersUri  = actor.Followers?.Id;
		user.IsCat         = actor.IsCat ?? false;
		user.Featured      = actor.Featured?.Link;
		user.Emojis        = []; //FIXME
		user.Tags          = []; //FIXME
		//TODO: FollowersCount
		//TODO: FollowingCount

		//TODO: update acct host via webfinger here

		var processPendingDeletes = await ResolveAvatarAndBanner(user, actor);

		user.UserProfile.Description = actor.MkSummary ?? await MfmConverter.FromHtmlAsync(actor.Summary);
		//user.UserProfile.Birthday = TODO;
		//user.UserProfile.Location = TODO;
		//user.UserProfile.Fields = TODO;
		user.UserProfile.UserHost = user.Host;
		user.UserProfile.Url      = actor.Url?.Link;

		db.Update(user);
		await db.SaveChangesAsync();
		await processPendingDeletes();
		return user;
	}

	public async Task<User> CreateLocalUserAsync(string username, string password, string? invite) {
		//TODO: invite system should allow multi-use invites & time limited invites
		if (security.Value.Registrations == Enums.Registrations.Closed)
			throw new GracefulException(HttpStatusCode.Forbidden, "Registrations are disabled on this server");
		if (security.Value.Registrations == Enums.Registrations.Invite && invite == null)
			throw new GracefulException(HttpStatusCode.Forbidden, "Request is missing the invite code");
		if (security.Value.Registrations == Enums.Registrations.Invite &&
		    !await db.RegistrationTickets.AnyAsync(p => p.Code == invite))
			throw new GracefulException(HttpStatusCode.Forbidden, "The specified invite code is invalid");
		if (username.Contains('.'))
			throw new GracefulException(HttpStatusCode.BadRequest, "Username must not contain the dot character");
		if (Constants.SystemUsers.Contains(username.ToLowerInvariant()))
			throw new GracefulException(HttpStatusCode.BadRequest, "Username must not be a system user");
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

		if (security.Value.Registrations == Enums.Registrations.Invite) {
			var ticket = await db.RegistrationTickets.FirstOrDefaultAsync(p => p.Code == invite);
			if (ticket == null)
				throw GracefulException.Forbidden("The specified invite code is invalid");
			db.Remove(ticket);
		}

		await db.AddRangeAsync(user, userKeypair, userProfile, usedUsername);
		await db.SaveChangesAsync();

		return user;
	}

	private async Task<Func<Task>> ResolveAvatarAndBanner(User user, ASActor actor) {
		var avatar = await driveSvc.StoreFile(actor.Avatar?.Url?.Link, user, actor.Avatar?.Sensitive ?? false);
		var banner = await driveSvc.StoreFile(actor.Banner?.Url?.Link, user, actor.Banner?.Sensitive ?? false);

		var prevAvatarId = user.AvatarId;
		var prevBannerId = user.BannerId;

		user.Avatar = avatar;
		user.Banner = banner;

		user.AvatarBlurhash = avatar?.Blurhash;
		user.BannerBlurhash = banner?.Blurhash;

		user.AvatarUrl = avatar?.Url;
		user.BannerUrl = banner?.Url;

		return async () => {
			if (prevAvatarId != null && avatar?.Id != prevAvatarId)
				await driveSvc.RemoveFile(prevAvatarId);

			if (prevBannerId != null && banner?.Id != prevBannerId)
				await driveSvc.RemoveFile(prevBannerId);
		};
	}
}