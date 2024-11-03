using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using AsyncKeyedLock;
using EntityFramework.Exceptions.Common;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;
using Iceshrimp.Backend.Core.Federation.WebFinger;
using Iceshrimp.Backend.Core.Helpers;
using Iceshrimp.Backend.Core.Helpers.LibMfm.Conversion;
using Iceshrimp.Backend.Core.Helpers.LibMfm.Parsing;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Backend.Core.Queues;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using static Iceshrimp.Parsing.MfmNodeTypes;

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
	FollowupTaskService followupTaskSvc,
	NotificationService notificationSvc,
	EmojiService emojiSvc,
	ActivityPub.MentionsResolver mentionsResolver,
	ActivityPub.UserRenderer userRenderer,
	QueueService queueSvc,
	EventService eventSvc,
	WebFingerService webFingerSvc,
	ActivityPub.FederationControlService fedCtrlSvc
)
{
	private static readonly AsyncKeyedLocker<string> KeyedLocker = new(o =>
	{
		o.PoolSize        = 100;
		o.PoolInitialFill = 5;
	});

	private static (string Username, string? Host) AcctToTuple(Uri acct)
	{
		if (acct.Scheme is not "acct") throw GracefulException.BadRequest($"Invalid query scheme: {acct}");
		var split = acct.AbsolutePath.Split('@');
		if (split.Length > 2) throw GracefulException.BadRequest($"Invalid query: {acct}");
		return split.Length != 2
			? (split[0], null)
			: (split[0], split[1].ToPunycodeLower());
	}

	private static (string Username, string? Host) AcctToTuple(string acct) => AcctToTuple(new Uri(acct));

	public async Task<User?> GetUserFromQueryAsync(Uri query, bool allowUrl)
	{
		if (query.Scheme is "https")
		{
			if (query.Host == instance.Value.WebDomain)
			{
				if (query.AbsolutePath.StartsWith("/users/"))
				{
					var userId = query.AbsolutePath["/users/".Length..];
					return await db.Users.IncludeCommonProperties().FirstOrDefaultAsync(p => p.Id == userId);
				}

				if (query.AbsolutePath.StartsWith("/@"))
				{
					var acct  = query.AbsolutePath[2..];
					var split = acct.Split('@');
					if (split.Length != 1)
						return await GetUserFromQueryAsync(new Uri($"acct:{acct}"), allowUrl);

					return await db.Users.IncludeCommonProperties()
					               .FirstOrDefaultAsync(p => p.Username == split[0].ToLower() && p.IsLocalUser);
				}

				return null;
			}

			var res = await db.Users.IncludeCommonProperties()
			                  .FirstOrDefaultAsync(p => p.Uri != null && p.Uri == query.AbsoluteUri);

			if (res != null || !allowUrl)
				return res;

			return await db.Users.IncludeCommonProperties()
			               .FirstOrDefaultAsync(p => p.UserProfile != null && p.UserProfile.Url == query.AbsoluteUri);
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

		var host = AcctToTuple(acct).Host ?? throw new Exception("Host must not be null at this stage");
		if (host == instance.Value.WebDomain || host == instance.Value.AccountDomain)
			throw GracefulException.UnprocessableEntity("Refusing to create remote user on local instance domain");
		if (await fedCtrlSvc.ShouldBlockAsync(uri, host))
			throw GracefulException.UnprocessableEntity("Refusing to create user on blocked instance");

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

		actor.Normalize(uri);

		user = await db.Users.FirstOrDefaultAsync(p => p.UsernameLower == actor.Username!.ToLowerInvariant() &&
		                                               p.Host == host);
		if (user is not null)
			throw GracefulException
				.UnprocessableEntity($"A user with acct @{user.UsernameLower}@{user.Host} already exists: {user.Uri}");

		if (actor.Id != uri)
			throw GracefulException.UnprocessableEntity("Uri doesn't match id of fetched actor");
		if (actor.PublicKey?.Id == null || actor.PublicKey?.PublicKey == null)
			throw GracefulException.UnprocessableEntity("Actor has no valid public key");
		if (new Uri(actor.PublicKey.Id).Host != new Uri(actor.Id).Host)
			throw GracefulException.UnprocessableEntity("Actor public key id host doesn't match actor id host");

		var emoji = await emojiSvc.ProcessEmojiAsync(actor.Tags?.OfType<ASEmoji>().ToList(), host);

		var fields = actor.Attachments != null
			? await actor.Attachments
			             .OfType<ASField>()
			             .Where(p => p is { Name: not null, Value: not null })
			             .Select(async p => new UserProfile.Field
			             {
				             Name = p.Name!, Value = await MfmConverter.FromHtmlAsync(p.Value) ?? ""
			             })
			             .AwaitAllAsync()
			: null;

		var bio = actor.MkSummary ?? await MfmConverter.FromHtmlAsync(actor.Summary);

		var tags = ResolveHashtags(bio, actor);

		user = new User
		{
			Id                  = IdHelpers.GenerateSnowflakeId(),
			CreatedAt           = DateTime.UtcNow,
			LastFetchedAt       = followupTaskSvc.IsBackgroundWorker ? null : DateTime.UtcNow,
			DisplayName         = actor.DisplayName,
			IsLocked            = actor.IsLocked ?? false,
			IsBot               = actor.IsBot,
			Username            = actor.Username!,
			UsernameLower       = actor.Username!.ToLowerInvariant(),
			Host                = host,
			MovedToUri          = actor.MovedTo?.Link,
			AlsoKnownAs         = actor.AlsoKnownAs?.Where(p => p.Link != null).Select(p => p.Link!).ToList(),
			IsExplorable        = actor.IsDiscoverable ?? false,
			Inbox               = actor.Inbox?.Link,
			SharedInbox         = actor.SharedInbox?.Link ?? actor.Endpoints?.SharedInbox?.Id,
			FollowersUri        = actor.Followers?.Id,
			Uri                 = actor.Id,
			IsCat               = actor.IsCat ?? false,
			Featured            = actor.Featured?.Id,
			SplitDomainResolved = true,
			//TODO: FollowersCount
			//TODO: FollowingCount
			Emojis = emoji.Select(p => p.Id).ToList(),
			Tags   = tags
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
			UserId = user.Id,
			KeyId  = actor.PublicKey.Id,
			KeyPem = actor.PublicKey.PublicKey
		};

		try
		{
			await db.AddRangeAsync(user, profile, publicKey);
			await db.SaveChangesAsync();
			var processPendingDeletes = await ResolveAvatarAndBanner(user, actor);
			await processPendingDeletes();
			user = await UpdateProfileMentions(user, actor);
			UpdateUserPinnedNotesInBackground(actor, user);
			_ = followupTaskSvc.ExecuteTask("UpdateInstanceUserCounter", async provider =>
			{
				var bgDb          = provider.GetRequiredService<DatabaseContext>();
				var bgInstanceSvc = provider.GetRequiredService<InstanceService>();

				var dbInstance = await bgInstanceSvc.GetUpdatedInstanceMetadataAsync(user);
				await bgDb.Instances.Where(p => p.Id == dbInstance.Id)
				          .ExecuteUpdateAsync(p => p.SetProperty(i => i.UsersCount, i => i.UsersCount + 1));
			});
			return user;
		}
		catch (UniqueConstraintException e) when (e.ConstraintProperties is [nameof(User.Uri)])
		{
			logger.LogError("Encountered UniqueConstraintException while creating user {uri}, attempting to refetch...",
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
		catch (UniqueConstraintException e)
		{
			logger.LogError("Failed to insert user: Unable to satisfy unique constraint: {constraint}",
			                e.ConstraintName);
			throw;
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
		if (user.IsLocalUser) return user;
		if (actor is { IsUnresolved: true } or { Username: null })
			actor = null; // This will trigger a fetch a couple lines down

		// Prevent multiple update jobs from running concurrently
		db.Update(user);
		var userId = user.Id;
		await db.Users.Where(u => u.Id == userId)
		        .ExecuteUpdateAsync(p => p.SetProperty(u => u.LastFetchedAt, DateTime.UtcNow));

		var uri = user.Uri ?? throw new Exception("Encountered remote user without a Uri");
		logger.LogDebug("Updating user with uri {uri}", uri);

		actor ??= await fetchSvc.FetchActorAsync(user.Uri);
		actor.Normalize(uri);

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
		user.Featured      = actor.Featured?.Id;

		var emoji = await emojiSvc.ProcessEmojiAsync(actor.Tags?.OfType<ASEmoji>().ToList(),
		                                             user.Host ??
		                                             throw new Exception("User host must not be null at this stage"));

		var fields = actor.Attachments != null
			? await actor.Attachments
			             .OfType<ASField>()
			             .Where(p => p is { Name: not null, Value: not null })
			             .Select(async p => new UserProfile.Field
			             {
				             Name = p.Name!, Value = await MfmConverter.FromHtmlAsync(p.Value) ?? ""
			             })
			             .AwaitAllAsync()
			: null;

		user.Emojis = emoji.Select(p => p.Id).ToList();
		//TODO: FollowersCount
		//TODO: FollowingCount

		//TODO: update acct host via webfinger here

		var processPendingDeletes = await ResolveAvatarAndBanner(user, actor);

		user.UserProfile.Description = actor.MkSummary ?? await MfmConverter.FromHtmlAsync(actor.Summary);
		//user.UserProfile.Birthday = TODO;
		//user.UserProfile.Location = TODO;
		user.UserProfile.Fields   = fields?.ToArray() ?? [];
		user.UserProfile.UserHost = user.Host;
		user.UserProfile.Url      = actor.Url?.Link;

		user.UserProfile.MentionsResolved = false;

		user.Tags = ResolveHashtags(user.UserProfile.Description, actor);
		user.Host = await UpdateUserHostAsync(user);

		db.Update(user);
		await db.SaveChangesAsync();
		await processPendingDeletes();
		user = await UpdateProfileMentions(user, actor, true);
		UpdateUserPinnedNotesInBackground(actor, user, true);
		return user;
	}

	public async Task<User> UpdateLocalUserAsync(User user, string? prevAvatarId, string? prevBannerId)
	{
		if (user.IsRemoteUser) throw new Exception("This method is only valid for local users");
		if (user.UserProfile == null) throw new Exception("user.UserProfile must not be null at this stage");

		user.Tags = ResolveHashtags(user.UserProfile.Description);

		user.Emojis = [];

		if (user.UserProfile.Description != null)
		{
			var nodes = MfmParser.Parse(user.UserProfile.Description);
			user.Emojis.AddRange((await emojiSvc.ResolveEmoji(nodes)).Select(p => p.Id).ToList());
		}

		if (user.DisplayName != null)
		{
			var nodes = MfmParser.Parse(user.DisplayName);
			user.Emojis.AddRange((await emojiSvc.ResolveEmoji(nodes)).Select(p => p.Id).ToList());
		}

		if (user.UserProfile.Fields.Length != 0)
		{
			var input = user.UserProfile.Fields.Select(p => $"{p.Name} {p.Value}");
			var nodes = MfmParser.Parse(string.Join('\n', input));
			user.Emojis.AddRange((await emojiSvc.ResolveEmoji(nodes)).Select(p => p.Id).ToList());
		}

		db.Update(user);
		db.Update(user.UserProfile);
		await db.SaveChangesAsync();

		user = await UpdateProfileMentions(user, null, wait: true);

		var activity = activityRenderer.RenderUpdate(await userRenderer.RenderAsync(user));
		await deliverSvc.DeliverToFollowersAsync(activity, user, []);

		_ = followupTaskSvc.ExecuteTask("UpdateLocalUserAsync", async provider =>
		{
			var bgDriveSvc = provider.GetRequiredService<DriveService>();
			if (prevAvatarId != null && user.Avatar?.Id != prevAvatarId)
				await bgDriveSvc.RemoveFile(prevAvatarId);
			if (prevBannerId != null && user.Banner?.Id != prevBannerId)
				await bgDriveSvc.RemoveFile(prevBannerId);
		});

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
		if (!Regex.IsMatch(username, @"^\w+$"))
			throw new GracefulException(HttpStatusCode.BadRequest, "Username must only contain letters and numbers");
		if (Constants.SystemUsers.Contains(username.ToLowerInvariant()))
			throw new GracefulException(HttpStatusCode.BadRequest, "Username must not be a system user");
		if (await db.Users.AnyAsync(p => p.IsLocalUser && p.UsernameLower == username.ToLowerInvariant()))
			throw new GracefulException(HttpStatusCode.BadRequest, "User already exists");
		if (await db.UsedUsernames.AnyAsync(p => p.Username.ToLower() == username.ToLowerInvariant()))
			throw new GracefulException(HttpStatusCode.BadRequest, "Username was already used");
		if (password.Length < 8)
			throw GracefulException.BadRequest("Password must be at least 8 characters long");

		var keypair = RSA.Create(4096);
		var user = new User
		{
			Id            = IdHelpers.GenerateSnowflakeId(),
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

		var userProfile  = new UserProfile { UserId  = user.Id };
		var userSettings = new UserSettings { UserId = user.Id, Password = AuthHelpers.HashPassword(password) };

		var usedUsername = new UsedUsername { CreatedAt = DateTime.UtcNow, Username = username.ToLowerInvariant() };

		if (security.Value.Registrations == Enums.Registrations.Invite)
		{
			var ticket = await db.RegistrationInvites.FirstOrDefaultAsync(p => p.Code == invite);
			if (ticket == null)
				throw GracefulException.Forbidden("The specified invite code is invalid");
			db.Remove(ticket);
		}

		await db.AddRangeAsync(user, userKeypair, userProfile, userSettings, usedUsername);
		await db.SaveChangesAsync();

		return user;
	}

	private async Task<Func<Task>> ResolveAvatarAndBanner(User user, ASActor actor)
	{
		var avatar = await driveSvc.StoreFile(actor.Avatar?.Url?.Link, user, actor.Avatar?.Sensitive ?? false,
		                                      logExisting: false);
		var banner = await driveSvc.StoreFile(actor.Banner?.Url?.Link, user, actor.Banner?.Sensitive ?? false,
		                                      logExisting: false);

		var prevAvatarId = user.AvatarId;
		var prevBannerId = user.BannerId;

		user.Avatar = avatar;
		user.Banner = banner;

		user.AvatarBlurhash = avatar?.Blurhash;
		user.BannerBlurhash = banner?.Blurhash;

		user.AvatarUrl = avatar?.Url;
		user.BannerUrl = banner?.Url;

		await db.SaveChangesAsync();

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

	public async Task<UserPublickey> UpdateUserPublicKeyAsync(User user)
	{
		var uri   = user.Uri ?? throw new Exception("Can't update public key of user without Uri");
		var actor = await fetchSvc.FetchActorAsync(uri);

		if (actor.PublicKey?.PublicKey == null)
			throw new Exception("Failed to update user public key: Invalid or missing public key");

		var key = await db.UserPublickeys.FirstOrDefaultAsync(p => p.User == user) ?? new UserPublickey { User = user };

		var insert = key.KeyId == null!;

		key.KeyId  = actor.PublicKey.Id;
		key.KeyPem = actor.PublicKey.PublicKey;

		if (insert) db.Add(key);
		else db.Update(key);
		await db.SaveChangesAsync();
		return key;
	}

	public async Task DeleteUserAsync(ASActor actor)
	{
		var user = await db.Users.FirstOrDefaultAsync(p => p.Uri == actor.Id && p.IsRemoteUser);

		if (user == null)
		{
			logger.LogDebug("User {uri} is unknown, skipping delete task", actor.Id);
			return;
		}

		await DeleteUserAsync(user);
	}

	public async Task DeleteUserAsync(User user)
	{
		await queueSvc.BackgroundTaskQueue.EnqueueAsync(new UserDeleteJobData { UserId = user.Id });
	}

	public async Task PurgeUserAsync(User user)
	{
		await queueSvc.BackgroundTaskQueue.EnqueueAsync(new UserPurgeJobData { UserId = user.Id });
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

		if (session.LastActiveDate != null && session.LastActiveDate > DateTime.UtcNow - TimeSpan.FromMinutes(5))
			return;

		_ = followupTaskSvc.ExecuteTask("UpdateSessionMetadata", async provider =>
		{
			var bgDb = provider.GetRequiredService<DatabaseContext>();
			await bgDb.Sessions.Where(p => p.Id == session.Id)
			          .ExecuteUpdateAsync(p => p.SetProperty(u => u.LastActiveDate, DateTime.UtcNow));
		});
	}

	private void UpdateUserLastActive(User user)
	{
		if (user.LastActiveDate != null && user.LastActiveDate > DateTime.UtcNow - TimeSpan.FromMinutes(5))
			return;

		_ = followupTaskSvc.ExecuteTask("UpdateUserLastActive", async provider =>
		{
			var bgDb = provider.GetRequiredService<DatabaseContext>();
			await bgDb.Users.Where(p => p.Id == user.Id)
			          .ExecuteUpdateAsync(p => p.SetProperty(u => u.LastActiveDate, DateTime.UtcNow));
		});
	}

	public async Task AcceptFollowRequestAsync(FollowRequest request)
	{
		if (request is { Follower.IsRemoteUser: true, RequestId: null })
			throw GracefulException.UnprocessableEntity("Cannot accept remote request without request id");

		var following = new Following
		{
			Id                  = IdHelpers.GenerateSnowflakeId(),
			CreatedAt           = DateTime.UtcNow,
			Follower            = request.Follower,
			Followee            = request.Followee,
			FollowerHost        = request.FollowerHost,
			FolloweeHost        = request.FolloweeHost,
			FollowerInbox       = request.FollowerInbox,
			FolloweeInbox       = request.FolloweeInbox,
			FollowerSharedInbox = request.FollowerSharedInbox,
			FolloweeSharedInbox = request.FolloweeSharedInbox,
			RelationshipId      = request.RelationshipId
		};

		await db.Users.Where(p => p.Id == request.Follower.Id)
		        .ExecuteUpdateAsync(p => p.SetProperty(i => i.FollowingCount, i => i.FollowingCount + 1));
		await db.Users.Where(p => p.Id == request.Followee.Id)
		        .ExecuteUpdateAsync(p => p.SetProperty(i => i.FollowersCount, i => i.FollowersCount + 1));

		db.Remove(request);
		await db.AddAsync(following);
		await db.SaveChangesAsync();

		if (request.Follower is { IsRemoteUser: true })
		{
			_ = followupTaskSvc.ExecuteTask("IncrementInstanceIncomingFollowsCounter", async provider =>
			{
				var bgDb          = provider.GetRequiredService<DatabaseContext>();
				var bgInstanceSvc = provider.GetRequiredService<InstanceService>();
				var dbInstance    = await bgInstanceSvc.GetUpdatedInstanceMetadataAsync(request.Follower);
				await bgDb.Instances.Where(p => p.Id == dbInstance.Id)
				          .ExecuteUpdateAsync(p => p.SetProperty(i => i.IncomingFollows, i => i.IncomingFollows + 1));
			});

			var activity = activityRenderer.RenderAccept(request.Followee, request.Follower, request.RequestId!);
			await deliverSvc.DeliverToAsync(activity, request.Followee, request.Follower);
		}
		else if (request.Followee is { IsRemoteUser: true })
		{
			_ = followupTaskSvc.ExecuteTask("IncrementInstanceOutgoingFollowsCounter", async provider =>
			{
				var bgDb          = provider.GetRequiredService<DatabaseContext>();
				var bgInstanceSvc = provider.GetRequiredService<InstanceService>();
				var dbInstance    = await bgInstanceSvc.GetUpdatedInstanceMetadataAsync(request.Followee);
				await bgDb.Instances.Where(p => p.Id == dbInstance.Id)
				          .ExecuteUpdateAsync(p => p.SetProperty(i => i.OutgoingFollows, i => i.OutgoingFollows + 1));
			});
		}

		if (request.Followee.IsRemoteUser && request.Follower.IsLocalUser && request.Followee.FollowersCount == 0)
			UpdateUserPinnedNotesInBackground(request.Followee);

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

	[SuppressMessage("ReSharper", "EntityFramework.UnsupportedServerSideFunctionCall", Justification = "Projectables")]
	public async Task FollowUserAsync(User follower, User followee, string? requestId = null)
	{
		if (follower.Id == followee.Id)
			throw GracefulException.UnprocessableEntity("You cannot follow yourself");
		if (follower.IsRemoteUser && followee.IsRemoteUser)
			throw GracefulException.UnprocessableEntity("Cannot process follow between two remote users");
		if (follower.IsSystemUser || followee.IsSystemUser)
			throw GracefulException.UnprocessableEntity("System users cannot have follow relationships");

		Guid? relationshipId = null;

		// If followee is remote, send a follow activity immediately
		if (followee.IsRemoteUser)
		{
			relationshipId = Guid.NewGuid();
			var activity = activityRenderer.RenderFollow(follower, followee, relationshipId);
			await deliverSvc.DeliverToAsync(activity, follower, followee);
		}

		// Check blocks separately for local/remote follower
		if (follower.IsRemoteUser)
		{
			if (requestId == null)
				throw GracefulException.UnprocessableEntity("Cannot process remote follow without requestId");

			if (await db.Users.AnyAsync(p => p == followee && p.IsBlocking(follower)))
			{
				var activity = activityRenderer.RenderReject(followee, follower, requestId);
				await deliverSvc.DeliverToAsync(activity, followee, follower);
				return;
			}
		}
		else
		{
			if (await db.Users.AnyAsync(p => p == followee && p.IsBlocking(follower)))
				throw GracefulException.UnprocessableEntity("You are not allowed to follow this user");
		}

		// We have to create a request instead of a follow relationship in these cases
		if (followee.IsLocked || followee.IsRemoteUser)
		{
			// We already have a pending follow request, so we want to update the request id in case it changed
			if (await db.FollowRequests.AnyAsync(p => p.Follower == follower && p.Followee == followee))
			{
				await db.FollowRequests.Where(p => p.Follower == follower && p.Followee == followee)
				        .ExecuteUpdateAsync(p => p.SetProperty(i => i.RequestId, _ => requestId));
			}
			else
			{
				// There already is an established follow relationship
				if (await db.Followings.AnyAsync(p => p.Follower == follower && p.Followee == followee))
				{
					// If the follower is remote, immediately send an accept activity, otherwise do nothing
					if (follower.IsRemoteUser)
					{
						if (requestId == null)
							throw new Exception("requestId must not be null at this stage");

						var activity = activityRenderer.RenderAccept(followee, follower, requestId);
						await deliverSvc.DeliverToAsync(activity, followee, follower);
					}
				}
				// Otherwise, create a new request and insert it into the database
				else
				{
					var autoAccept = followee.IsLocalUser &&
					                 await db.Followings.AnyAsync(p => p.Follower == followee &&
					                                                   p.Followee == follower &&
					                                                   p.Follower.UserSettings != null &&
					                                                   p.Follower.UserSettings.AutoAcceptFollowed);

					// Followee has auto accept enabled & is already following the follower user
					if (autoAccept)
					{
						if (follower.IsRemoteUser)
						{
							if (requestId == null)
								throw new Exception("requestId must not be null at this stage");

							var activity = activityRenderer.RenderAccept(followee, follower, requestId);
							await deliverSvc.DeliverToAsync(activity, followee, follower);
						}

						var following = new Following
						{
							Id                  = IdHelpers.GenerateSnowflakeId(),
							CreatedAt           = DateTime.UtcNow,
							Followee            = followee,
							Follower            = follower,
							FolloweeHost        = followee.Host,
							FollowerHost        = follower.Host,
							FolloweeInbox       = followee.Inbox,
							FollowerInbox       = follower.Inbox,
							FolloweeSharedInbox = followee.SharedInbox,
							FollowerSharedInbox = follower.SharedInbox
						};

						await db.AddAsync(following);
						await db.SaveChangesAsync();
						await notificationSvc.GenerateFollowNotification(follower, followee);

						await db.Users.Where(p => p.Id == follower.Id)
						        .ExecuteUpdateAsync(p => p.SetProperty(i => i.FollowingCount,
						                                               i => i.FollowingCount + 1));
						await db.Users.Where(p => p.Id == followee.Id)
						        .ExecuteUpdateAsync(p => p.SetProperty(i => i.FollowersCount,
						                                               i => i.FollowersCount + 1));

						if (follower.IsRemoteUser)
						{
							_ = followupTaskSvc.ExecuteTask("IncrementInstanceIncomingFollowsCounter", async provider =>
							{
								var bgDb          = provider.GetRequiredService<DatabaseContext>();
								var bgInstanceSvc = provider.GetRequiredService<InstanceService>();
								var dbInstance    = await bgInstanceSvc.GetUpdatedInstanceMetadataAsync(follower);
								await bgDb.Instances.Where(p => p.Id == dbInstance.Id)
								          .ExecuteUpdateAsync(p => p.SetProperty(i => i.IncomingFollows,
									                              i => i.IncomingFollows + 1));
							});
						}

						return;
					}

					var request = new FollowRequest
					{
						Id                  = IdHelpers.GenerateSnowflakeId(),
						CreatedAt           = DateTime.UtcNow,
						RequestId           = requestId,
						Followee            = followee,
						Follower            = follower,
						FolloweeHost        = followee.Host,
						FollowerHost        = follower.Host,
						FolloweeInbox       = followee.Inbox,
						FollowerInbox       = follower.Inbox,
						FolloweeSharedInbox = followee.SharedInbox,
						FollowerSharedInbox = follower.SharedInbox,
						RelationshipId      = relationshipId
					};

					await db.AddAsync(request);
					await db.SaveChangesAsync();
					await notificationSvc.GenerateFollowRequestReceivedNotification(request);
				}
			}
		}
		// Followee is local and not locked
		else
		{
			// If there isn't an established follow relationship already, create one
			if (!await db.Followings.AnyAsync(p => p.Follower == follower && p.Followee == followee))
			{
				var following = new Following
				{
					Id                  = IdHelpers.GenerateSnowflakeId(),
					CreatedAt           = DateTime.UtcNow,
					Followee            = followee,
					Follower            = follower,
					FolloweeHost        = followee.Host,
					FollowerHost        = follower.Host,
					FolloweeInbox       = followee.Inbox,
					FollowerInbox       = follower.Inbox,
					FolloweeSharedInbox = followee.SharedInbox,
					FollowerSharedInbox = follower.SharedInbox
				};

				await db.AddAsync(following);
				await db.SaveChangesAsync();
				await notificationSvc.GenerateFollowNotification(follower, followee);

				await db.Users.Where(p => p.Id == follower.Id)
				        .ExecuteUpdateAsync(p => p.SetProperty(i => i.FollowingCount, i => i.FollowingCount + 1));
				await db.Users.Where(p => p.Id == followee.Id)
				        .ExecuteUpdateAsync(p => p.SetProperty(i => i.FollowersCount, i => i.FollowersCount + 1));

				if (follower.IsRemoteUser)
				{
					_ = followupTaskSvc.ExecuteTask("IncrementInstanceIncomingFollowsCounter", async provider =>
					{
						var bgDb          = provider.GetRequiredService<DatabaseContext>();
						var bgInstanceSvc = provider.GetRequiredService<InstanceService>();
						var dbInstance    = await bgInstanceSvc.GetUpdatedInstanceMetadataAsync(follower);
						await bgDb.Instances.Where(p => p.Id == dbInstance.Id)
						          .ExecuteUpdateAsync(p => p.SetProperty(i => i.IncomingFollows,
						                                                 i => i.IncomingFollows + 1));
					});
				}
			}

			// If follower is remote, send an accept activity
			if (follower.IsRemoteUser)
			{
				if (requestId == null)
					throw new Exception("requestId must not be null at this stage");

				var activity = activityRenderer.RenderAccept(followee, follower, requestId);
				await deliverSvc.DeliverToAsync(activity, followee, follower);
			}
		}
	}

	/// <remarks>
	///     Make sure to call .PrecomputeRelationshipData(user) on the database query for the followee
	/// </remarks>
	public async Task RemoveFromFollowersAsync(User user, User follower)
	{
		if ((follower.PrecomputedIsFollowing ?? false) && follower.IsRemoteUser)
		{
			var activity = activityRenderer.RenderReject(userRenderer.RenderLite(user),
			                                             activityRenderer.RenderFollow(follower, user, null));
			await deliverSvc.DeliverToAsync(activity, user, follower);
		}

		if (follower.PrecomputedIsFollowing ?? false)
		{
			var followers = await db.Followings
			                        .Where(p => p.Followee == user && p.Follower == follower)
			                        .ToListAsync();

			await db.Users
			        .Where(p => p.Id == user.Id)
			        .ExecuteUpdateAsync(p => p.SetProperty(i => i.FollowersCount,
			                                               i => i.FollowersCount - followers.Count));

			await db.Users
			        .Where(p => p.Id == follower.Id)
			        .ExecuteUpdateAsync(p => p.SetProperty(i => i.FollowingCount,
			                                               i => i.FollowingCount - followers.Count));

			db.RemoveRange(followers);
			await db.SaveChangesAsync();

			if (follower.IsRemoteUser)
			{
				_ = followupTaskSvc.ExecuteTask("DecrementInstanceIncomingFollowsCounter", async provider =>
				{
					var bgDb          = provider.GetRequiredService<DatabaseContext>();
					var bgInstanceSvc = provider.GetRequiredService<InstanceService>();
					var dbInstance =
						await bgInstanceSvc.GetUpdatedInstanceMetadataAsync(follower);
					await bgDb.Instances.Where(p => p.Id == dbInstance.Id)
					          .ExecuteUpdateAsync(p => p.SetProperty(i => i.IncomingFollows,
					                                                 i => i.IncomingFollows - 1));
				});
			}

			follower.PrecomputedIsFollowedBy = false;
			eventSvc.RaiseUserUnfollowed(this, follower, user);
		}
	}

	/// <remarks>
	///     Make sure to call .PrecomputeRelationshipData(user) on the database query for the followee
	/// </remarks>
	public async Task UnfollowUserAsync(User user, User followee)
	{
		if (((followee.PrecomputedIsFollowedBy ?? false) || (followee.PrecomputedIsRequestedBy ?? false)) &&
		    followee.IsRemoteUser)
		{
			var relationshipId = await db.Followings.Where(p => p.Follower == user && p.Followee == followee)
			                             .Select(p => p.RelationshipId)
			                             .FirstOrDefaultAsync() ??
			                     await db.FollowRequests.Where(p => p.Follower == user && p.Followee == followee)
			                             .Select(p => p.RelationshipId)
			                             .FirstOrDefaultAsync();

			var activity = activityRenderer.RenderUnfollow(user, followee, relationshipId);
			await deliverSvc.DeliverToAsync(activity, user, followee);
		}

		if (followee.PrecomputedIsFollowedBy ?? false)
		{
			var followings = await db.Followings.Where(p => p.Follower == user && p.Followee == followee).ToListAsync();

			await db.Users.Where(p => p.Id == user.Id)
			        .ExecuteUpdateAsync(p => p.SetProperty(i => i.FollowingCount,
			                                               i => i.FollowingCount - followings.Count));
			await db.Users.Where(p => p.Id == followee.Id)
			        .ExecuteUpdateAsync(p => p.SetProperty(i => i.FollowersCount,
			                                               i => i.FollowersCount - followings.Count));

			db.RemoveRange(followings);
			await db.SaveChangesAsync();

			if (followee.IsRemoteUser)
			{
				_ = followupTaskSvc.ExecuteTask("DecrementInstanceOutgoingFollowsCounter", async provider =>
				{
					var bgDb          = provider.GetRequiredService<DatabaseContext>();
					var bgInstanceSvc = provider.GetRequiredService<InstanceService>();
					var dbInstance =
						await bgInstanceSvc.GetUpdatedInstanceMetadataAsync(followee);
					await bgDb.Instances.Where(p => p.Id == dbInstance.Id)
					          .ExecuteUpdateAsync(p => p.SetProperty(i => i.OutgoingFollows,
					                                                 i => i.OutgoingFollows - 1));
				});
			}

			followee.PrecomputedIsFollowedBy = false;
			eventSvc.RaiseUserUnfollowed(this, user, followee);
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

	[SuppressMessage("ReSharper", "SuggestBaseTypeForParameter", Justification = "Method only makes sense for users")]
	private void UpdateUserPinnedNotesInBackground(ASActor actor, User user, bool force = false)
	{
		if (followupTaskSvc.IsBackgroundWorker && !force) return;
		if (KeyedLocker.IsInUse($"pinnedNotes:{user.Id}")) return;
		_ = followupTaskSvc.ExecuteTask("UpdateUserPinnedNotes", async provider =>
		{
			using (await KeyedLocker.LockAsync($"pinnedNotes:{user.Id}"))
			{
				var bgDb      = provider.GetRequiredService<DatabaseContext>();
				var bgNoteSvc = provider.GetRequiredService<NoteService>();
				var bgUser    = await bgDb.Users.IncludeCommonProperties().FirstOrDefaultAsync(p => p.Id == user.Id);
				if (bgUser == null) return;
				await bgNoteSvc.UpdatePinnedNotesAsync(actor, bgUser);
			}
		});
	}

	[SuppressMessage("ReSharper", "SuggestBaseTypeForParameter", Justification = "Method only makes sense for users")]
	private void UpdateUserPinnedNotesInBackground(User user, bool force = false)
	{
		if (user.Uri == null) return;
		if (!user.IsRemoteUser) return;
		if (followupTaskSvc.IsBackgroundWorker && !force) return;
		if (KeyedLocker.IsInUse($"pinnedNotes:{user.Id}")) return;
		_ = followupTaskSvc.ExecuteTask("UpdateUserPinnedNotes", async provider =>
		{
			using (await KeyedLocker.LockAsync($"pinnedNotes:{user.Id}"))
			{
				var bgDb      = provider.GetRequiredService<DatabaseContext>();
				var bgNoteSvc = provider.GetRequiredService<NoteService>();
				var bgUser    = await bgDb.Users.IncludeCommonProperties().FirstOrDefaultAsync(p => p.Id == user.Id);
				if (bgUser == null) return;
				var bgFetch = provider.GetRequiredService<ActivityPub.ActivityFetcherService>();
				var actor   = await bgFetch.FetchActorAsync(user.Uri);
				await bgNoteSvc.UpdatePinnedNotesAsync(actor, bgUser);
			}
		});
	}

	[SuppressMessage("ReSharper", "EntityFramework.NPlusOne.IncompleteDataQuery", Justification = "Projectables")]
	[SuppressMessage("ReSharper", "EntityFramework.NPlusOne.IncompleteDataUsage", Justification = "Same as above")]
	[SuppressMessage("ReSharper", "SuggestBaseTypeForParameter", Justification = "Method only makes sense for users")]
	private async Task<User> UpdateProfileMentions(User user, ASActor? actor, bool force = false, bool wait = false)
	{
		if (followupTaskSvc.IsBackgroundWorker && !force) return user;
		if (KeyedLocker.IsInUse($"profileMentions:{user.Id}")) return user;

		var success = false;

		var task = followupTaskSvc.ExecuteTask("UpdateProfileMentionsInBackground", async provider =>
		{
			using (await KeyedLocker.LockAsync($"profileMentions:{user.Id}"))
			{
				var bgDbContext = provider.GetRequiredService<DatabaseContext>();
				var bgMentionsResolver = provider.GetRequiredService<UserProfileMentionsResolver>();
				var userId = user.Id;
				var bgUser = await bgDbContext.Users.IncludeCommonProperties().FirstOrDefaultAsync(p => p.Id == userId);
				if (bgUser?.UserProfile == null) return;

				if (actor != null)
				{
					var (mentions, splitDomainMapping) = await bgMentionsResolver.ResolveMentions(actor, bgUser.Host);
					var fields = actor.Attachments != null
						? await actor.Attachments
						             .OfType<ASField>()
						             .Where(p => p is { Name: not null, Value: not null })
						             .Select(async p => new UserProfile.Field
						             {
							             Name  = p.Name!,
							             Value = await MfmConverter.FromHtmlAsync(p.Value, mentions) ?? ""
						             })
						             .AwaitAllAsync()
						: null;

					var description = actor.MkSummary != null
						? mentionsResolver.ResolveMentions(actor.MkSummary, bgUser.Host, mentions, splitDomainMapping)
						: await MfmConverter.FromHtmlAsync(actor.Summary, mentions);

					bgUser.UserProfile.Mentions    = mentions;
					bgUser.UserProfile.Fields      = fields?.ToArray() ?? [];
					bgUser.UserProfile.Description = description;
				}
				else
				{
					bgUser.UserProfile.Mentions = await bgMentionsResolver.ResolveMentions(bgUser.UserProfile.Fields,
						bgUser.UserProfile.Description, bgUser.Host);
				}

				bgUser.UserProfile.MentionsResolved = true;
				bgDbContext.Update(bgUser.UserProfile);
				await bgDbContext.SaveChangesAsync();
				success = true;
			}
		});

		if (wait)
			await task;
		else
			await task.SafeWaitAsync(TimeSpan.FromMilliseconds(500));

		if (success)
			await db.ReloadEntityRecursivelyAsync(user);

		return user;
	}

	private List<string> ResolveHashtags(string? text, ASActor? actor = null)
	{
		List<string> tags = [];

		if (text != null)
		{
			tags = MfmParser.Parse(text)
			                .SelectMany(p => p.Children.Append(p))
			                .OfType<MfmHashtagNode>()
			                .Select(p => p.Hashtag.ToLowerInvariant())
			                .Select(p => p.Trim('#'))
			                .Distinct()
			                .ToList();
		}

		var extracted = actor?.Tags?.OfType<ASHashtag>()
		                     .Select(p => p.Name?.ToLowerInvariant())
		                     .Where(p => p != null)
		                     .Cast<string>()
		                     .Select(p => p.Trim('#'))
		                     .Distinct()
		                     .ToList();

		if (extracted != null)
			tags.AddRange(extracted);

		if (tags.Count == 0) return [];

		tags = tags.Distinct().ToList();

		_ = followupTaskSvc.ExecuteTask("UpdateHashtagsTable", async provider =>
		{
			var bgDb     = provider.GetRequiredService<DatabaseContext>();
			var existing = await bgDb.Hashtags.Where(p => tags.Contains(p.Name)).Select(p => p.Name).ToListAsync();
			var dbTags = tags.Except(existing)
			                 .Select(p => new Hashtag { Id = IdHelpers.GenerateSnowflakeId(), Name = p });
			await bgDb.UpsertRange(dbTags).On(p => p.Name).NoUpdate().RunAsync();
		});

		return tags;
	}

	public async Task MuteUserAsync(User muter, User mutee, DateTime? expiration)
	{
		mutee.PrecomputedIsMutedBy = true;

		var muting = await db.Mutings.FirstOrDefaultAsync(p => p.Muter == muter && p.Mutee == mutee);

		if (muting != null)
		{
			if (muting.ExpiresAt == expiration) return;
			muting.ExpiresAt = expiration;
			await db.SaveChangesAsync();
			if (expiration == null) return;
			var job = new MuteExpiryJobData { MuteId = muting.Id };
			await queueSvc.BackgroundTaskQueue.ScheduleAsync(job, expiration.Value);
			return;
		}

		muting = new Muting
		{
			Id        = IdHelpers.GenerateSnowflakeId(),
			CreatedAt = DateTime.UtcNow,
			Mutee     = mutee,
			Muter     = muter,
			ExpiresAt = expiration
		};
		await db.AddAsync(muting);
		await db.SaveChangesAsync();

		eventSvc.RaiseUserMuted(this, muter, mutee);

		if (expiration != null)
		{
			var job = new MuteExpiryJobData { MuteId = muting.Id };
			await queueSvc.BackgroundTaskQueue.ScheduleAsync(job, expiration.Value);
		}
	}

	public async Task UnmuteUserAsync(User muter, User mutee)
	{
		if (!mutee.PrecomputedIsMutedBy ?? false)
			return;

		await db.Mutings.Where(p => p.Muter == muter && p.Mutee == mutee).ExecuteDeleteAsync();
		eventSvc.RaiseUserUnmuted(this, muter, mutee);

		mutee.PrecomputedIsMutedBy = false;
	}

	public async Task BlockUserAsync(User blocker, User blockee)
	{
		if (blockee.PrecomputedIsBlockedBy ?? false) return;
		blockee.PrecomputedIsBlockedBy = true;

		var blocking = new Blocking
		{
			Id        = IdHelpers.GenerateSnowflakeId(),
			CreatedAt = DateTime.UtcNow,
			Blockee   = blockee,
			Blocker   = blocker
		};

		await db.FollowRequests.Where(p => p.Follower == blockee && p.Followee == blocker).ExecuteDeleteAsync();
		await db.FollowRequests.Where(p => p.Follower == blocker && p.Followee == blockee).ExecuteDeleteAsync();

		var cnt1 = await db.Followings.Where(p => p.Follower == blockee && p.Followee == blocker).ExecuteDeleteAsync();
		var cnt2 = await db.Followings.Where(p => p.Follower == blocker && p.Followee == blockee).ExecuteDeleteAsync();

		await db.Users.Where(p => p == blocker)
		        .ExecuteUpdateAsync(p => p.SetProperty(i => i.FollowersCount, i => i.FollowersCount - cnt1)
		                                  .SetProperty(i => i.FollowingCount, i => i.FollowingCount - cnt2));
		await db.Users.Where(p => p == blockee)
		        .ExecuteUpdateAsync(p => p.SetProperty(i => i.FollowersCount, i => i.FollowersCount - cnt2)
		                                  .SetProperty(i => i.FollowingCount, i => i.FollowingCount - cnt1));

		// clean up notifications
		await db.Notifications.Where(p => ((p.Notifiee == blocker &&
		                                    p.Notifier == blockee) ||
		                                   (p.Notifiee == blockee &&
		                                    p.Notifier == blocker)) &&
		                                  (p.Type == Notification.NotificationType.Follow ||
		                                   p.Type == Notification.NotificationType.FollowRequestAccepted ||
		                                   p.Type == Notification.NotificationType.FollowRequestReceived))
		        .ExecuteDeleteAsync();

		await db.AddAsync(blocking);
		await db.SaveChangesAsync();

		eventSvc.RaiseUserBlocked(this, blocker, blockee);

		if (blocker.IsLocalUser && blockee.IsRemoteUser)
		{
			var actor    = userRenderer.RenderLite(blocker);
			var obj      = userRenderer.RenderLite(blockee);
			var activity = activityRenderer.RenderBlock(actor, obj, blocking.Id);
			await deliverSvc.DeliverToAsync(activity, blocker, blockee);
		}
	}

	public async Task UnblockUserAsync(User blocker, User blockee)
	{
		if (!blockee.PrecomputedIsBlockedBy ?? false)
			return;

		blockee.PrecomputedIsBlockedBy = false;

		var blocking = await db.Blockings.FirstOrDefaultAsync(p => p.Blocker == blocker && p.Blockee == blockee);
		if (blocking == null) return;

		db.Remove(blocking);
		await db.SaveChangesAsync();

		eventSvc.RaiseUserUnblocked(this, blocker, blockee);

		if (blocker.IsLocalUser && blockee.IsRemoteUser)
		{
			var actor    = userRenderer.RenderLite(blocker);
			var obj      = userRenderer.RenderLite(blockee);
			var block    = activityRenderer.RenderBlock(actor, obj, blocking.Id);
			var activity = activityRenderer.RenderUndo(actor, block);
			await deliverSvc.DeliverToAsync(activity, blocker, blockee);
		}
	}

	public async Task AddAliasAsync(User user, User alias)
	{
		if (user.IsRemoteUser) throw GracefulException.BadRequest("Cannot add alias for remote user");
		if (user.Id == alias.Id) throw GracefulException.BadRequest("You cannot add an alias to yourself");

		user.AlsoKnownAs ??= [];
		var uri = alias.Uri ?? alias.GetPublicUri(instance.Value);

		if (!user.AlsoKnownAs.Contains(uri)) user.AlsoKnownAs.Add(uri);
		await UpdateLocalUserAsync(user, user.AvatarId, user.BannerId);
	}

	public async Task RemoveAliasAsync(User user, string aliasUri)
	{
		if (user.IsRemoteUser) throw GracefulException.BadRequest("Cannot manage aliases for remote user");
		if (user.AlsoKnownAs is null or []) return;
		if (!user.AlsoKnownAs.Contains(aliasUri)) return;

		user.AlsoKnownAs.RemoveAll(p => p == aliasUri);
		await UpdateLocalUserAsync(user, user.AvatarId, user.BannerId);
	}

	public async Task MoveToUserAsync(User source, User target)
	{
		if (source.IsRemoteUser) throw GracefulException.BadRequest("Cannot initiate move for remote user");
		if (source.Id == target.Id) throw GracefulException.BadRequest("You cannot migrate to yourself");

		target = await UpdateUserAsync(target, force: true);
		if (target.AlsoKnownAs is null || !target.AlsoKnownAs.Contains(source.GetPublicUri(instance.Value)))
			throw GracefulException.BadRequest("Target user has not added you as an account alias");

		var sourceUri = source.Uri ?? source.GetPublicUri(instance.Value);
		var targetUri = target.Uri ?? target.GetPublicUri(instance.Value);
		if (source.MovedToUri is not null && source.MovedToUri != targetUri)
			throw GracefulException.BadRequest("You can only initiate repeated migrations to the same target account");

		source.MovedToUri = targetUri;
		await db.SaveChangesAsync();

		var move = activityRenderer.RenderMove(userRenderer.RenderLite(source), userRenderer.RenderLite(target));
		await deliverSvc.DeliverToFollowersAsync(move, source, []);
		await MoveRelationshipsAsync(source, target, sourceUri, targetUri);
	}

	public async Task UndoMoveAsync(User user)
	{
		if (user.MovedToUri is null) return;
		user.MovedToUri = null;
		await UpdateLocalUserAsync(user, user.AvatarId, user.BannerId);
	}

	private async Task<string?> UpdateUserHostAsync(User user)
	{
		if (user.IsLocalUser || user.Uri == null || user.SplitDomainResolved)
			return user.Host;

		var res   = await webFingerSvc.ResolveAsync(user.Uri);
		var match = res?.Links.FirstOrDefault(p => p is { Rel: "self", Type: "application/activity+json" })?.Href;
		if (res == null || match != user.Uri)
		{
			logger.LogWarning("Updating split domain host failed for user {id}: uri mismatch (pass 1) - '{uri}' <> '{match}'",
			                  user.Id, user.Uri, match);
			return user.Host;
		}

		var acct = ActivityPub.UserResolver.GetAcctUri(res);
		if (acct == null)
		{
			logger.LogWarning("Updating split domain host failed for user {id}: acct was null", user.Id);
			return user.Host;
		}

		var split = acct.Split('@');
		if (split.Length != 2)
		{
			logger.LogWarning("Updating split domain host failed for user {id}: invalid acct - '{acct}'",
			                  user.Id, acct);
			return user.Host;
		}

		if (user.Host == split[1])
		{
			user.SplitDomainResolved = true;
			return user.Host;
		}

		logger.LogDebug("Updating split domain for user {id}: {host} -> {newHost}", user.Id, user.Host, split[1]);

		res   = await webFingerSvc.ResolveAsync(acct);
		match = res?.Links.FirstOrDefault(p => p is { Rel: "self", Type: "application/activity+json" })?.Href;
		if (res == null || match != user.Uri)
		{
			logger.LogWarning("Updating split domain host failed for user {id}: uri mismatch (pass 2) - '{uri}' <> '{match}'",
			                  user.Id, user.Uri, match);
			return user.Host;
		}

		if (acct != ActivityPub.UserResolver.GetAcctUri(res))
		{
			logger.LogWarning("Updating split domain host failed for user {id}: subject mismatch - '{acct}' <> '{subject}'",
			                  user.Id, acct, res.Subject.TrimStart('@'));
			return user.Host;
		}

		user.SplitDomainResolved = true;
		return split[1];
	}

	public async Task MoveRelationshipsAsync(User source, User target, string sourceUri, string targetUri)
	{
		var followers = db.Followings
		                  .Where(p => p.Followee == source && p.Follower.IsLocalUser)
		                  .Select(p => p.Follower)
		                  .AsChunkedAsyncEnumerable(50, p => p.Id, hook: p => p.PrecomputeRelationshipData(source));

		await foreach (var follower in followers)
		{
			try
			{
				if (follower.Id == target.Id) continue;

				await FollowUserAsync(follower, target);

				// We need to transfer the precomputed properties to the source user for each follower so that the unfollow method works correctly
				source.PrecomputedIsFollowedBy  = follower.PrecomputedIsFollowing;
				source.PrecomputedIsRequestedBy = follower.PrecomputedIsRequested;

				await UnfollowUserAsync(follower, source);
			}
			catch (Exception e)
			{
				logger.LogWarning("Failed to process move ({sourceUri} -> {targetUri}) for follower {id}: {error}",
				                  sourceUri, targetUri, follower.Id, e);
			}
		}

		if (source.IsRemoteUser || target.IsRemoteUser) return;

		var following = db.Followings
		                  .Where(p => p.Follower == source)
		                  .Select(p => p.Follower)
		                  .AsChunkedAsyncEnumerable(50, p => p.Id, hook: p => p.PrecomputeRelationshipData(source));

		await foreach (var followee in following)
		{
			try
			{
				await FollowUserAsync(target, followee);
				await UnfollowUserAsync(source, followee);
			}
			catch (Exception e)
			{
				logger.LogWarning("Failed to process move ({sourceUri} -> {targetUri}) for followee {id}: {error}",
				                  sourceUri, targetUri, followee.Id, e);
			}
		}
	}

	public async Task SuspendUserAsync(User user)
	{
		if (user.IsSuspended) return;
		user.IsSuspended = true;
		await db.SaveChangesAsync();
	}

	public async Task UnsuspendUserAsync(User user)
	{
		if (!user.IsSuspended) return;
		user.IsSuspended = false;
		await db.SaveChangesAsync();
	}
}