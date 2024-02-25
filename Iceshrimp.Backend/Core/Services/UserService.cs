using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Security.Cryptography;
using EntityFramework.Exceptions.Common;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;
using Iceshrimp.Backend.Core.Helpers;
using Iceshrimp.Backend.Core.Helpers.LibMfm.Conversion;
using Iceshrimp.Backend.Core.Middleware;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Core.Services;

public class UserService(
	[SuppressMessage("ReSharper", "SuggestBaseTypeForParameterInConstructor")]
	IOptionsSnapshot<Config.SecuritySection> security,
	IOptions<Config.InstanceSection> instance,
	ILogger<UserService> logger,
	DatabaseContext db,
	ActivityPub.ActivityFetcherService fetchSvc,
	ActivityPub.ActivityRenderer activityRenderer,
	ActivityPub.ActivityDeliverService deliverSvc,
	DriveService driveSvc,
	MfmConverter mfmConverter,
	FollowupTaskService followupTaskSvc,
	NotificationService notificationSvc,
	EmojiService emojiSvc
)
{
	private (string Username, string? Host) AcctToTuple(string acct)
	{
		if (!acct.StartsWith("acct:")) throw new GracefulException(HttpStatusCode.BadRequest, "Invalid query");

		var split = acct[5..].Split('@');
		if (split.Length != 2)
			return (split[0], instance.Value.AccountDomain.ToPunycode());

		return (split[0], split[1].ToPunycode());
	}

	public async Task<User?> GetUserFromQueryAsync(string query)
	{
		if (query.StartsWith("http://") || query.StartsWith("https://"))
			if (query.StartsWith($"https://{instance.Value.WebDomain}/users/"))
			{
				query = query[$"https://{instance.Value.WebDomain}/users/".Length..];
				return await db.Users.IncludeCommonProperties().FirstOrDefaultAsync(p => p.Id == query) ??
				       throw GracefulException.NotFound("User not found");
			}
			else
			{
				return await db.Users
				               .IncludeCommonProperties()
				               .FirstOrDefaultAsync(p => p.Uri != null && p.Uri.ToLower() == query.ToLowerInvariant());
			}

		var tuple = AcctToTuple(query);
		if (tuple.Host == instance.Value.WebDomain || tuple.Host == instance.Value.AccountDomain)
			tuple.Host = null;
		return await db.Users
		               .IncludeCommonProperties()
		               .FirstOrDefaultAsync(p => p.UsernameLower == tuple.Username.ToLowerInvariant() &&
		                                         p.Host == tuple.Host);
	}

	public async Task<User> CreateUserAsync(string uri, string acct)
	{
		logger.LogDebug("Creating user {acct} with uri {uri}", acct, uri);

		var user = await db.Users
		                   .IncludeCommonProperties()
		                   .FirstOrDefaultAsync(p => p.Uri != null && p.Uri == uri);

		if (user != null)
		{
			// Another thread got there first
			logger.LogDebug("Actor {uri} is already known, returning existing user {id}", user.Uri, user.Id);
			return user;
		}

		var actor = await fetchSvc.FetchActorAsync(uri);
		logger.LogDebug("Got actor: {url}", actor.Url);

		actor.Normalize(uri, acct);

		if (actor.Id != uri)
			throw GracefulException.UnprocessableEntity("Uri doesn't match id of fetched actor");

		if (actor.PublicKey?.Id == null || actor.PublicKey?.PublicKey == null)
			throw GracefulException.UnprocessableEntity("Actor has no valid public key");

		var host = AcctToTuple(acct).Host ?? throw new Exception("Host must not be null at this stage");

		var emoji = await emojiSvc.ProcessEmojiAsync(actor.Tags?.OfType<ASEmoji>().ToList(), host);
		var tags = actor.Tags?.OfType<ASHashtag>()
		                .Select(p => p.Name?.ToLowerInvariant())
		                .Where(p => p != null)
		                .Cast<string>()
		                .ToList();

		var fields = actor.Attachments != null
			? await actor.Attachments
			             .OfType<ASField>()
			             .Where(p => p is { Name: not null, Value: not null })
			             .Select(async p => new UserProfile.Field
			             {
				             Name = p.Name!, Value = await mfmConverter.FromHtmlAsync(p.Value) ?? ""
			             })
			             .AwaitAllAsync()
			: null;

		var bio = actor.MkSummary ?? await mfmConverter.FromHtmlAsync(actor.Summary);

		user = new User
		{
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
			AlsoKnownAs   = actor.AlsoKnownAs?.Where(p => p.Link != null).Select(p => p.Link!).ToList(),
			IsExplorable  = actor.IsDiscoverable ?? false,
			Inbox         = actor.Inbox?.Link,
			SharedInbox   = actor.SharedInbox?.Link ?? actor.Endpoints?.SharedInbox?.Id,
			FollowersUri  = actor.Followers?.Id,
			Uri           = actor.Id,
			IsCat         = actor.IsCat ?? false,
			Featured      = actor.Featured?.Link,
			//TODO: FollowersCount
			//TODO: FollowingCount
			Emojis = emoji.Select(p => p.Id).ToList(),
			Tags   = tags ?? []
		};

		var profile = new UserProfile
		{
			User        = user,
			Description = bio,
			//Birthday = TODO,
			//Location = TODO,
			Fields   = fields?.ToArray() ?? [],
			UserHost = user.Host,
			Url      = actor.Url?.Link
		};

		var publicKey = new UserPublickey
		{
			UserId = user.Id, KeyId = actor.PublicKey.Id, KeyPem = actor.PublicKey.PublicKey
		};

		try
		{
			await db.AddRangeAsync(user, profile, publicKey);

			// We need to do this after calling db.Add(Range) to ensure data consistency
			var processPendingDeletes = await ResolveAvatarAndBanner(user, actor);
			await db.SaveChangesAsync();
			await processPendingDeletes();
			await UpdateProfileMentionsInBackground(user);
			return user;
		}
		catch (UniqueConstraintException)
		{
			logger.LogDebug("Encountered UniqueConstraintException while creating user {uri}, attempting to refetch...",
			                user.Uri);
			// another thread got there first, so we need to return the existing user
			var res = await db.Users
			                  .IncludeCommonProperties()
			                  .FirstOrDefaultAsync(p => p.Uri != null && p.Uri == user.Uri);

			// something else must have went wrong, rethrow exception
			if (res == null)
			{
				logger.LogError("Fetching user {uri} failed, rethrowing exception", user.Uri);
				throw;
			}

			logger.LogDebug("Successfully fetched user {uri}", user.Uri);

			return res;
		}
	}

	public async Task<User> UpdateUserAsync(string id)
	{
		var user = await db.Users.IncludeCommonProperties().FirstOrDefaultAsync(p => p.Id == id) ??
		           throw new Exception("Cannot update nonexistent user");
		return await UpdateUserAsync(user, force: true);
	}

	public async Task<User> UpdateUserAsync(User user, ASActor? actor = null, bool force = false)
	{
		if (!user.NeedsUpdate && actor == null && !force) return user;
		if (actor is { IsUnresolved: true } or { Username: null })
			actor = null; // This will trigger a fetch a couple lines down

		// Prevent multiple update jobs from running concurrently
		db.Update(user);
		await db.Users.Where(u => u.Id == user.Id)
		        .ExecuteUpdateAsync(p => p.SetProperty(u => u.LastFetchedAt, DateTime.UtcNow));

		var uri = user.Uri ?? throw new Exception("Encountered remote user without a Uri");
		logger.LogDebug("Updating user with uri {uri}", uri);

		actor ??= await fetchSvc.FetchActorAsync(user.Uri);
		actor.Normalize(uri, user.AcctWithPrefix);

		user.UserProfile ??= await db.UserProfiles.FirstOrDefaultAsync(p => p.User == user);
		user.UserProfile ??= new UserProfile { User = user };

		user.LastFetchedAt = DateTime.UtcNow; // If we don't do this we'll overwrite the value with the previous one
		user.Inbox         = actor.Inbox?.Link;
		user.SharedInbox   = actor.SharedInbox?.Link ?? actor.Endpoints?.SharedInbox?.Id;
		user.DisplayName   = actor.DisplayName;
		user.IsLocked      = actor.IsLocked ?? false;
		user.IsBot         = actor.IsBot;
		user.MovedToUri    = actor.MovedTo?.Link;
		user.AlsoKnownAs   = actor.AlsoKnownAs?.Where(p => p.Link != null).Select(p => p.Link!).ToList();
		user.IsExplorable  = actor.IsDiscoverable ?? false;
		user.FollowersUri  = actor.Followers?.Id;
		user.IsCat         = actor.IsCat ?? false;
		user.Featured      = actor.Featured?.Link;

		var emoji = await emojiSvc.ProcessEmojiAsync(actor.Tags?.OfType<ASEmoji>().ToList(),
		                                             user.Host ??
		                                             throw new Exception("User host must not be null at this stage"));

		var tags = actor.Tags?.OfType<ASHashtag>()
		                .Select(p => p.Name?.ToLowerInvariant())
		                .Where(p => p != null)
		                .Cast<string>()
		                .ToList();

		var fields = actor.Attachments != null
			? await actor.Attachments
			             .OfType<ASField>()
			             .Where(p => p is { Name: not null, Value: not null })
			             .Select(async p => new UserProfile.Field
			             {
				             Name = p.Name!, Value = await mfmConverter.FromHtmlAsync(p.Value) ?? ""
			             })
			             .AwaitAllAsync()
			: null;

		user.Emojis = emoji.Select(p => p.Id).ToList();
		user.Tags   = tags ?? [];
		//TODO: FollowersCount
		//TODO: FollowingCount

		//TODO: update acct host via webfinger here

		var processPendingDeletes = await ResolveAvatarAndBanner(user, actor);

		user.UserProfile.Description = actor.MkSummary ?? await mfmConverter.FromHtmlAsync(actor.Summary);
		//user.UserProfile.Birthday = TODO;
		//user.UserProfile.Location = TODO;
		user.UserProfile.Fields   = fields?.ToArray() ?? [];
		user.UserProfile.UserHost = user.Host;
		user.UserProfile.Url      = actor.Url?.Link;

		db.Update(user);
		await db.SaveChangesAsync();
		await processPendingDeletes();
		await UpdateProfileMentionsInBackground(user);
		return user;
	}

	public async Task<User> CreateLocalUserAsync(string username, string password, string? invite)
	{
		//TODO: invite system should allow multi-use invites & time limited invites
		if (security.Value.Registrations == Enums.Registrations.Closed)
			throw new GracefulException(HttpStatusCode.Forbidden, "Registrations are disabled on this server");
		if (security.Value.Registrations == Enums.Registrations.Invite && invite == null)
			throw new GracefulException(HttpStatusCode.Forbidden, "Request is missing the invite code");
		if (security.Value.Registrations == Enums.Registrations.Invite &&
		    !await db.RegistrationInvites.AnyAsync(p => p.Code == invite))
			throw new GracefulException(HttpStatusCode.Forbidden, "The specified invite code is invalid");
		if (username.Contains('.'))
			throw new GracefulException(HttpStatusCode.BadRequest, "Username must not contain the dot character");
		if (Constants.SystemUsers.Contains(username.ToLowerInvariant()))
			throw new GracefulException(HttpStatusCode.BadRequest, "Username must not be a system user");
		if (await db.Users.AnyAsync(p => p.Host == null && p.UsernameLower == username.ToLowerInvariant()))
			throw new GracefulException(HttpStatusCode.BadRequest, "User already exists");
		if (await db.UsedUsernames.AnyAsync(p => p.Username.ToLower() == username.ToLowerInvariant()))
			throw new GracefulException(HttpStatusCode.BadRequest, "Username was already used");
		if (password.Length < 8)
			throw GracefulException.BadRequest("Password must be at least 8 characters long");

		var keypair = RSA.Create(4096);
		var user = new User
		{
			Id            = IdHelpers.GenerateSlowflakeId(),
			CreatedAt     = DateTime.UtcNow,
			Username      = username,
			UsernameLower = username.ToLowerInvariant(),
			Host          = null
		};

		var userKeypair = new UserKeypair
		{
			UserId     = user.Id,
			PrivateKey = keypair.ExportPkcs8PrivateKeyPem(),
			PublicKey  = keypair.ExportSubjectPublicKeyInfoPem()
		};

		var userProfile = new UserProfile { UserId = user.Id, Password = AuthHelpers.HashPassword(password) };

		var usedUsername = new UsedUsername { CreatedAt = DateTime.UtcNow, Username = username.ToLowerInvariant() };

		if (security.Value.Registrations == Enums.Registrations.Invite)
		{
			var ticket = await db.RegistrationInvites.FirstOrDefaultAsync(p => p.Code == invite);
			if (ticket == null)
				throw GracefulException.Forbidden("The specified invite code is invalid");
			db.Remove(ticket);
		}

		await db.AddRangeAsync(user, userKeypair, userProfile, usedUsername);
		await db.SaveChangesAsync();

		return user;
	}

	private async Task<Func<Task>> ResolveAvatarAndBanner(User user, ASActor actor)
	{
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

		return async () =>
		{
			if (prevAvatarId != null && avatar?.Id != prevAvatarId)
				await driveSvc.RemoveFile(prevAvatarId);

			if (prevBannerId != null && banner?.Id != prevBannerId)
				await driveSvc.RemoveFile(prevBannerId);
		};
	}

	public async Task<UserPublickey> UpdateUserPublicKeyAsync(UserPublickey key)
	{
		var uri   = key.User.Uri ?? throw new Exception("Can't update public key of user without Uri");
		var actor = await fetchSvc.FetchActorAsync(uri);

		if (actor.PublicKey?.PublicKey == null)
			throw new Exception("Failed to update user public key: Invalid or missing public key");

		key.KeyId  = actor.PublicKey.Id;
		key.KeyPem = actor.PublicKey.PublicKey;

		db.Update(key);
		await db.SaveChangesAsync();
		return key;
	}

	public async Task DeleteUserAsync(ASActor actor)
	{
		var user = await db.Users
		                   .Include(user => user.Avatar)
		                   .Include(user => user.Banner)
		                   .FirstOrDefaultAsync(p => p.Uri == actor.Id && p.Host != null);

		if (user == null)
		{
			logger.LogDebug("User {uri} is unknown, skipping delete task", actor.Id);
			return;
		}

		db.Remove(user);
		await db.SaveChangesAsync();

		if (user.Avatar != null)
			await driveSvc.RemoveFile(user.Avatar);
		if (user.Banner != null)
			await driveSvc.RemoveFile(user.Banner);
	}

	public void UpdateOauthTokenMetadata(OauthToken token)
	{
		UpdateUserLastActive(token.User);

		if (token.LastActiveDate != null && token.LastActiveDate > DateTime.UtcNow - TimeSpan.FromHours(1)) return;

		_ = followupTaskSvc.ExecuteTask("UpdateOauthTokenMetadata", async provider =>
		{
			var bgDb = provider.GetRequiredService<DatabaseContext>();
			await bgDb.OauthTokens.Where(p => p.Id == token.Id)
			          .ExecuteUpdateAsync(p => p.SetProperty(u => u.LastActiveDate, DateTime.UtcNow));
		});
	}

	public void UpdateSessionMetadata(Session session)
	{
		UpdateUserLastActive(session.User);

		_ = followupTaskSvc.ExecuteTask("UpdateSessionMetadata", async provider =>
		{
			var bgDb = provider.GetRequiredService<DatabaseContext>();
			await bgDb.Sessions.Where(p => p.Id == session.Id)
			          .ExecuteUpdateAsync(p => p.SetProperty(u => u.LastActiveDate, DateTime.UtcNow));
		});
	}

	private void UpdateUserLastActive(User user)
	{
		if (user.LastActiveDate != null && user.LastActiveDate > DateTime.UtcNow - TimeSpan.FromHours(1)) return;

		_ = followupTaskSvc.ExecuteTask("UpdateUserLastActive", async provider =>
		{
			var bgDb = provider.GetRequiredService<DatabaseContext>();
			await bgDb.Users.Where(p => p.Id == user.Id)
			          .ExecuteUpdateAsync(p => p.SetProperty(u => u.LastActiveDate, DateTime.UtcNow));
		});
	}

	public async Task AcceptFollowRequestAsync(FollowRequest request)
	{
		if (request.FollowerHost != null)
		{
			var requestId = request.RequestId ?? throw new Exception("Cannot accept request without request id");
			var activity  = activityRenderer.RenderAccept(request.Followee, request.Follower, requestId);
			await deliverSvc.DeliverToAsync(activity, request.Followee, request.Follower);
		}

		var following = new Following
		{
			Id                  = IdHelpers.GenerateSlowflakeId(),
			CreatedAt           = DateTime.UtcNow,
			Follower            = request.Follower,
			Followee            = request.Followee,
			FollowerHost        = request.FollowerHost,
			FolloweeHost        = request.FolloweeHost,
			FollowerInbox       = request.FollowerInbox,
			FolloweeInbox       = request.FolloweeInbox,
			FollowerSharedInbox = request.FollowerSharedInbox,
			FolloweeSharedInbox = request.FolloweeSharedInbox
		};

		request.Followee.FollowersCount++;
		request.Follower.FollowingCount++;

		db.Remove(request);
		await db.AddAsync(following);
		await db.SaveChangesAsync();

		await notificationSvc.GenerateFollowNotification(request.Follower, request.Followee);
		await notificationSvc.GenerateFollowRequestAcceptedNotification(request);

		// Clean up notifications
		await db.Notifications
		        .Where(p => p.Type == Notification.NotificationType.FollowRequestReceived &&
		                    p.Notifiee == request.Followee &&
		                    p.Notifier == request.Follower)
		        .ExecuteDeleteAsync();
	}

	public async Task RejectFollowRequestAsync(FollowRequest request)
	{
		if (request.FollowerHost != null)
		{
			var requestId = request.RequestId ?? throw new Exception("Cannot reject request without request id");
			var activity  = activityRenderer.RenderReject(request.Followee, request.Follower, requestId);
			await deliverSvc.DeliverToAsync(activity, request.Followee, request.Follower);
		}

		db.Remove(request);
		await db.SaveChangesAsync();

		// Clean up notifications
		await db.Notifications
		        .Where(p => ((p.Type == Notification.NotificationType.FollowRequestReceived ||
		                      p.Type == Notification.NotificationType.Follow) &&
		                     p.Notifiee == request.Followee &&
		                     p.Notifier == request.Follower) ||
		                    (p.Type == Notification.NotificationType.FollowRequestAccepted &&
		                     p.Notifiee == request.Follower &&
		                     p.Notifier == request.Followee))
		        .ExecuteDeleteAsync();
	}

	public async Task FollowUserAsync(User user, User followee)
	{
		if (followee.Host != null)
		{
			var activity = activityRenderer.RenderFollow(user, followee);
			await deliverSvc.DeliverToAsync(activity, user, followee);
		}

		if (followee.IsLocked || followee.Host != null)
		{
			var request = new FollowRequest
			{
				Id                  = IdHelpers.GenerateSlowflakeId(),
				CreatedAt           = DateTime.UtcNow,
				Followee            = followee,
				Follower            = user,
				FolloweeHost        = followee.Host,
				FollowerHost        = user.Host,
				FolloweeInbox       = followee.Inbox,
				FollowerInbox       = user.Inbox,
				FolloweeSharedInbox = followee.SharedInbox,
				FollowerSharedInbox = user.SharedInbox
			};

			await db.AddAsync(request);
		}
		else
		{
			var following = new Following
			{
				Id                  = IdHelpers.GenerateSlowflakeId(),
				CreatedAt           = DateTime.UtcNow,
				Followee            = followee,
				Follower            = user,
				FolloweeHost        = followee.Host,
				FollowerHost        = user.Host,
				FolloweeInbox       = followee.Inbox,
				FollowerInbox       = user.Inbox,
				FolloweeSharedInbox = followee.SharedInbox,
				FollowerSharedInbox = user.SharedInbox
			};

			await db.AddAsync(following);
		}

		// If user is local & not locked, we need to increment following/follower counts here,
		// otherwise we'll do it when receiving the Accept activity / the local followee accepts the request
		if (followee.Host == null && !followee.IsLocked)
		{
			followee.FollowersCount++;
			user.FollowingCount++;
		}

		await db.SaveChangesAsync();

		if (followee.Host == null && !followee.IsLocked)
			await notificationSvc.GenerateFollowNotification(user, followee);
	}

	/// <remarks>
	///     Make sure to call .PrecomputeRelationshipData(user) on the database query for the followee
	/// </remarks>
	public async Task UnfollowUserAsync(User user, User followee)
	{
		if ((followee.PrecomputedIsFollowedBy ?? false) || (followee.PrecomputedIsRequestedBy ?? false))
		{
			if (followee.Host != null)
			{
				var activity = activityRenderer.RenderUnfollow(user, followee);
				await deliverSvc.DeliverToAsync(activity, user, followee);
			}
		}

		if (followee.PrecomputedIsFollowedBy ?? false)
		{
			var followings = await db.Followings.Where(p => p.Follower == user && p.Followee == followee).ToListAsync();
			user.FollowingCount     -= followings.Count;
			followee.FollowersCount -= followings.Count;
			db.RemoveRange(followings);
			await db.SaveChangesAsync();

			followee.PrecomputedIsFollowedBy = false;
		}

		if (followee.PrecomputedIsRequestedBy ?? false)
		{
			await db.FollowRequests.Where(p => p.Follower == user && p.Followee == followee).ExecuteDeleteAsync();
			followee.PrecomputedIsRequestedBy = false;
		}

		// Clean up notifications
		await db.Notifications
		        .Where(p => (p.Type == Notification.NotificationType.FollowRequestAccepted &&
		                     p.Notifiee == user &&
		                     p.Notifier == followee) ||
		                    (p.Type == Notification.NotificationType.Follow &&
		                     p.Notifiee == followee &&
		                     p.Notifier == user))
		        .ExecuteDeleteAsync();

		// Clean up user list memberships
		await db.UserListMembers.Where(p => p.UserList.User == user && p.User == followee).ExecuteDeleteAsync();
	}

	[SuppressMessage("ReSharper", "EntityFramework.NPlusOne.IncompleteDataQuery", Justification = "Projectables")]
	[SuppressMessage("ReSharper", "EntityFramework.NPlusOne.IncompleteDataUsage", Justification = "Same as above")]
	[SuppressMessage("ReSharper", "SuggestBaseTypeForParameter", Justification = "Method only makes sense for users")]
	private async Task UpdateProfileMentionsInBackground(User user)
	{
		var task = followupTaskSvc.ExecuteTask("UpdateProfileMentionsInBackground", async provider =>
		{
			var bgDbContext = provider.GetRequiredService<DatabaseContext>();
			var bgMentionsResolver = (followupTaskSvc.ServiceProvider ?? provider)
				.GetRequiredService<UserProfileMentionsResolver>();
			var bgUser = await bgDbContext.Users.IncludeCommonProperties().FirstOrDefaultAsync(p => p.Id == user.Id);
			if (bgUser?.UserProfile == null) return;
			bgUser.UserProfile.Mentions =
				await bgMentionsResolver.ResolveMentions(bgUser.UserProfile.Fields, bgUser.UserProfile.Description,
				                                         bgUser.Host);
			bgDbContext.Update(bgUser.UserProfile);
			await bgDbContext.SaveChangesAsync();
		});

		if (followupTaskSvc.IsBackgroundWorker)
			await task;
	}
}