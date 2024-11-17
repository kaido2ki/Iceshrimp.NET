using System.Diagnostics.CodeAnalysis;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;
using Iceshrimp.Backend.Core.Helpers;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Backend.Core.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using static Iceshrimp.Backend.Core.Federation.ActivityPub.UserResolver;

namespace Iceshrimp.Backend.Core.Federation.ActivityPub;

[SuppressMessage("ReSharper", "SuggestBaseTypeForParameter",
                 Justification = "We want to enforce AS types, so we can't use the base type here")]
public class ActivityHandlerService(
	ILogger<ActivityHandlerService> logger,
	NoteService noteSvc,
	UserService userSvc,
	UserResolver userResolver,
	DatabaseContext db,
	IOptions<Config.InstanceSection> config,
	FederationControlService federationCtrl,
	NotificationService notificationSvc,
	ObjectResolver objectResolver,
	FollowupTaskService followupTaskSvc,
	EmojiService emojiSvc,
	EventService eventSvc,
	RelayService relaySvc
) : IScopedService
{
	public async Task PerformActivityAsync(ASActivity activity, string? inboxUserId, string? authenticatedUserId)
	{
		logger.LogDebug("Processing activity: {activity}", activity.Id);
		if (activity.Actor == null)
			throw GracefulException.UnprocessableEntity("Cannot perform activity as actor 'null'");
		if (await federationCtrl.ShouldBlockAsync(activity.Actor.Id))
			throw new InstanceBlockedException(activity.Actor.Id);
		if (activity.Object == null && activity is not ASBite)
			throw GracefulException.UnprocessableEntity("Activity object is null");

		var resolvedActor = await userResolver.ResolveAsync(activity.Actor.Id, EnforceUriFlags);
		if (authenticatedUserId == null)
			throw GracefulException.UnprocessableEntity("Refusing to process activity without authenticatedUserId");

		if (resolvedActor.Id != authenticatedUserId)
		{
			logger.LogDebug("Authenticated user id {authenticatedUserId} doesn't match resolved actor id {resolvedActorId}, skipping",
			                authenticatedUserId, resolvedActor.Id);
			return;
		}

		if (resolvedActor.IsLocalUser)
			throw GracefulException.UnprocessableEntity("Refusing to process activity for local user");

		if (new Uri(activity.Actor.Id).Host != new Uri(activity.Id).Host)
			throw GracefulException
				.UnprocessableEntity($"Activity identifier ({activity.Id}) host doesn't match actor identifier ({activity.Actor.Id}) host");
		if (resolvedActor.Host == null || resolvedActor.Uri == null)
			throw new Exception("resolvedActor.Host and resolvedActor.Uri must not be null at this stage");

		UpdateInstanceMetadataInBackground(resolvedActor.Host, new Uri(resolvedActor.Uri).Host);

		if (resolvedActor.IsSuspended && activity is not ASDelete)
			throw GracefulException.UnprocessableEntity("Actor is suspended");

		var inboxUser = inboxUserId != null
			? await db.Users.IncludeCommonProperties().FirstOrDefaultAsync(p => p.Id == inboxUserId)
			: null;

		var task = activity switch
		{
			ASAccept accept     => HandleAccept(accept, resolvedActor),
			ASAnnounce announce => HandleAnnounce(announce, resolvedActor),
			ASBite bite         => HandleBite(bite, resolvedActor, inboxUser),
			ASBlock block       => HandleBlock(block, resolvedActor),
			ASCreate create     => HandleCreate(create, resolvedActor, inboxUser),
			ASDelete delete     => HandleDelete(delete, resolvedActor),
			ASEmojiReact react  => HandleReact(react, resolvedActor),
			ASFollow follow     => HandleFollow(follow, resolvedActor),
			ASLike like         => HandleLike(like, resolvedActor),
			ASMove move         => HandleMove(move, resolvedActor),
			ASReject reject     => HandleReject(reject, resolvedActor),
			ASUndo undo         => HandleUndo(undo, resolvedActor),
			ASUnfollow unfollow => HandleUnfollow(unfollow, resolvedActor),
			ASUpdate update     => HandleUpdate(update, resolvedActor),

			// Separated for readability
			_ => throw GracefulException.UnprocessableEntity($"Activity type {activity.Type} is unknown")
		};

		await task;
	}

	private void UpdateInstanceMetadataInBackground(string host, string webDomain)
	{
		_ = followupTaskSvc.ExecuteTask("UpdateInstanceMetadata", async provider =>
		{
			var instanceSvc = provider.GetRequiredService<InstanceService>();
			await instanceSvc.UpdateInstanceStatusAsync(host, webDomain);
		});
	}

	private async Task HandleCreate(ASCreate activity, User actor, User? inboxUser)
	{
		if (activity.Object == null)
			throw GracefulException.UnprocessableEntity("Create activity object was null");

		activity.Object = await objectResolver.ResolveObject(activity.Object, actor.Uri) as ASNote ??
		                  throw GracefulException.UnprocessableEntity("Failed to resolve create object");

		using (await NoteService.GetNoteProcessLockAsync(activity.Object.Id))
			await noteSvc.ProcessNoteAsync(activity.Object, actor, inboxUser);
	}

	private async Task HandleDelete(ASDelete activity, User resolvedActor)
	{
		if (activity.Object == null)
			throw GracefulException.UnprocessableEntity("Delete activity object was null");

		activity.Object = await objectResolver.ResolveObject(activity.Object, resolvedActor.Uri);

		switch (activity.Object)
		{
			case ASActor actor when !await db.Users.AnyAsync(p => p.Uri == actor.Id):
				logger.LogDebug("Delete activity object {id} is unknown, skipping", actor.Id);
				return;
			case ASActor actor when activity.Actor?.Id != actor.Id:
				throw GracefulException.UnprocessableEntity("Refusing to delete user: actor doesn't match");
			case ASActor actor:
				await userSvc.DeleteUserAsync(actor);
				break;
			case ASNote note:
				await noteSvc.DeleteNoteAsync(note, resolvedActor);
				break;
			case ASTombstone tombstone when await db.Notes.AnyAsync(p => p.Uri == tombstone.Id):
				await noteSvc.DeleteNoteAsync(tombstone, resolvedActor);
				return;
			case ASTombstone tombstone when await db.Users.AnyAsync(p => p.Uri == tombstone.Id):
			{
				if (tombstone.Id != activity.Actor?.Id)
					throw GracefulException.UnprocessableEntity("Refusing to delete user: actor doesn't match");

				await userSvc.DeleteUserAsync(activity.Actor);
				break;
			}
			case ASTombstone tombstone:
				logger.LogDebug("Delete activity object {id} is unknown, skipping", tombstone.Id);
				break;
			default:
				logger.LogDebug("Delete activity object {id} couldn't be resolved, skipping", activity.Id);
				break;
		}
	}

	private async Task HandleFollow(ASFollow activity, User resolvedActor)
	{
		if (activity.Object == null)
			throw GracefulException.UnprocessableEntity("Follow activity object was null");

		activity.Object = await objectResolver.ResolveObject(activity.Object, resolvedActor.Uri);
		if (activity.Object is not ASActor obj)
			throw GracefulException.UnprocessableEntity("Follow activity object is invalid");

		var followee = await userResolver.ResolveAsync(obj.Id, EnforceUriFlags);
		if (followee.IsRemoteUser)
			throw GracefulException.UnprocessableEntity("Cannot process follow for remote followee");

		await userSvc.FollowUserAsync(resolvedActor, followee, activity.Id);
	}

	private async Task HandleUnfollow(ASUnfollow activity, User resolvedActor)
	{
		if (activity.Object == null)
			throw GracefulException.UnprocessableEntity("Unfollow activity object was null");

		activity.Object = await objectResolver.ResolveObject(activity.Object, resolvedActor.Uri);
		if (activity.Object is not ASActor obj)
			throw GracefulException.UnprocessableEntity("Unfollow activity object is invalid");

		await UnfollowAsync(obj, resolvedActor);
	}

	private async Task HandleAccept(ASAccept activity, User actor)
	{
		if (activity.Object == null)
			throw GracefulException.UnprocessableEntity("Accept activity object was null");

		activity.Object = await objectResolver.ResolveObject(activity.Object, actor.Uri);
		if (activity.Object is not ASFollow obj)
			throw GracefulException.UnprocessableEntity("Accept activity object is invalid");

		var relayPrefix = $"https://{config.Value.WebDomain}/activities/follow-relay/";
		if (obj.Id.StartsWith(relayPrefix))
		{
			await relaySvc.HandleAccept(actor, obj.Id[relayPrefix.Length..]);
			return;
		}

		var prefix = $"https://{config.Value.WebDomain}/follows/";
		if (!obj.Id.StartsWith(prefix))
			throw GracefulException.UnprocessableEntity($"Object id '{obj.Id}' not a valid follow request id");

		var ids = obj.Id[prefix.Length..].TrimEnd('/').Split("/");
		if (ids.Length < 2)
			throw GracefulException
				.UnprocessableEntity("Failed to parse ASAccept activity: ASFollow id doesn't have enough components");
		if (ids[1] != actor.Id)
			throw GracefulException
				.UnprocessableEntity($"Actor id '{actor.Id}' doesn't match followee id '{ids[1]}'");

		var request = await db.FollowRequests
		                      .Include(p => p.Follower.UserProfile)
		                      .Include(p => p.Followee.UserProfile)
		                      .FirstOrDefaultAsync(p => p.Followee == actor && p.FollowerId == ids[0]);

		if (request == null)
		{
			if (await db.Followings.AnyAsync(p => p.Followee == actor && p.FollowerId == ids[0]))
				return;

			throw GracefulException.UnprocessableEntity($"No follow or follow request matching follower '{ids[0]}'" +
			                                            $"and followee '{actor.Id}' found");
		}

		await userSvc.AcceptFollowRequestAsync(request);
	}

	private async Task HandleReject(ASReject activity, User resolvedActor)
	{
		if (activity.Actor == null)
			throw GracefulException.UnprocessableEntity("Reject activity actor was null");
		if (activity.Object == null)
			throw GracefulException.UnprocessableEntity("Reject activity object was null");

		activity.Object = await objectResolver.ResolveObject(activity.Object, resolvedActor.Uri);
		if (activity.Object is not ASFollow follow)
			throw GracefulException.UnprocessableEntity("Reject activity object is invalid");

		var relayPrefix = $"https://{config.Value.WebDomain}/activities/follow-relay/";
		if (follow.Id.StartsWith(relayPrefix))
		{
			await relaySvc.HandleReject(resolvedActor, follow.Id[relayPrefix.Length..]);
			return;
		}

		if (follow is not { Actor: not null })
			throw GracefulException.UnprocessableEntity("Refusing to reject object with invalid follow object");

		var resolvedFollower = await userResolver.ResolveAsync(follow.Actor.Id, EnforceUriFlags);
		if (resolvedFollower is not { IsLocalUser: true })
			throw GracefulException.UnprocessableEntity("Refusing to reject remote follow");
		if (resolvedActor.Uri == null)
			throw GracefulException.UnprocessableEntity("Refusing to process reject for actor without uri");
		if (resolvedActor.Uri != follow.Object?.Id)
			throw GracefulException.UnprocessableEntity("Refusing to process reject: actor doesn't match object");

		await db.FollowRequests.Where(p => p.Followee == resolvedActor && p.Follower == resolvedFollower)
		        .ExecuteDeleteAsync();
		var count = await db.Followings.Where(p => p.Followee == resolvedActor && p.Follower == resolvedFollower)
		                    .ExecuteDeleteAsync();
		if (count > 0)
		{
			await db.Users.Where(p => p.Id == resolvedFollower.Id)
			        .ExecuteUpdateAsync(p => p.SetProperty(i => i.FollowingCount, i => i.FollowingCount - count));
			await db.Users.Where(p => p.Id == resolvedActor.Id)
			        .ExecuteUpdateAsync(p => p.SetProperty(i => i.FollowersCount, i => i.FollowersCount - count));
			await db.SaveChangesAsync();
		}

		await db.Notifications
		        .Where(p => p.Type == Notification.NotificationType.FollowRequestAccepted)
		        .Where(p => p.Notifiee == resolvedFollower &&
		                    p.Notifier == resolvedActor)
		        .ExecuteDeleteAsync();

		await db.UserListMembers
		        .Where(p => p.UserList.User == resolvedFollower && p.User == resolvedActor)
		        .ExecuteDeleteAsync();
	}

	private async Task HandleUndo(ASUndo activity, User resolvedActor)
	{
		if (activity.Object == null)
			throw GracefulException.UnprocessableEntity("Undo activity object was null");

		activity.Object = await objectResolver.ResolveObject(activity.Object, resolvedActor.Uri);

		switch (activity.Object)
		{
			case ASFollow { Object: ASActor followee }:
				await UnfollowAsync(followee, resolvedActor);
				break;
			case ASLike { Object: ASNote note } like:
				if ((like.Content ?? like.MisskeyReaction) is { } reaction)
					await noteSvc.RemoveReactionFromNoteAsync(note, resolvedActor, reaction);
				else
					await noteSvc.UnlikeNoteAsync(note, resolvedActor);
				break;
			case ASAnnounce { Object: ASNote note }:
				await noteSvc.UndoAnnounceAsync(note, resolvedActor);
				break;
			case ASEmojiReact { Object: ASNote note } react:
				await noteSvc.RemoveReactionFromNoteAsync(note, resolvedActor, react.Content);
				break;
			case ASBlock { Object: ASActor blockee }:
				await UnblockAsync(resolvedActor, blockee);
				break;
			case null:
				logger.LogDebug("Unknown undo activity object, skipping");
				break;
			default:
				logger.LogDebug("Unknown undo activity object {id} of type {type}, skipping", activity.Object?.Id,
				                activity.Object?.Type);
				break;
		}
	}

	private async Task HandleLike(ASLike activity, User resolvedActor)
	{
		if (resolvedActor.Host == null)
			throw GracefulException.UnprocessableEntity("Cannot process like for local actor");
		if (activity.Object == null)
			throw GracefulException.UnprocessableEntity("Like activity object was null");

		activity.Object = await objectResolver.ResolveObject(activity.Object, resolvedActor.Uri);

		if (activity.Object is not ASNote note)
		{
			logger.LogDebug("Like activity object is unknown, skipping");
			return;
		}

		if (activity.MisskeyReaction != null)
		{
			await emojiSvc.ProcessEmojiAsync(activity.Tags?.OfType<ASEmoji>().ToList(), resolvedActor.Host);
			await noteSvc.ReactToNoteAsync(note, resolvedActor, activity.MisskeyReaction);
		}
		else
		{
			await noteSvc.LikeNoteAsync(note, resolvedActor);
		}
	}

	private async Task HandleUpdate(ASUpdate activity, User resolvedActor)
	{
		if (activity.Actor == null)
			throw GracefulException.UnprocessableEntity("Cannot process update for null actor");
		if (activity.Object == null)
			throw GracefulException.UnprocessableEntity("Update activity object was null");

		activity.Object = await objectResolver.ResolveObject(activity.Object, resolvedActor.Uri);

		switch (activity.Object)
		{
			case ASActor actor:
				if (actor.Id != activity.Actor.Id)
					throw GracefulException.UnprocessableEntity("Refusing to update actor with mismatching id");
				await userSvc.UpdateUserAsync(resolvedActor, actor);
				break;
			case ASNote note:
				using (await NoteService.GetNoteProcessLockAsync(note.Id))
					await noteSvc.ProcessNoteUpdateAsync(note, resolvedActor);
				break;
			default:
				logger.LogDebug("Like activity object is unknown, skipping");
				break;
		}
	}

	private async Task HandleBite(ASBite activity, User resolvedActor, User? inboxUser)
	{
		var target = await objectResolver.ResolveObject(activity.Target, resolvedActor.Uri);
		var dbBite = target switch
		{
			ASActor targetActor => new Bite
			{
				Id         = IdHelpers.GenerateSnowflakeId(activity.PublishedAt),
				CreatedAt  = activity.PublishedAt ?? DateTime.UtcNow,
				Uri        = activity.Id,
				User       = resolvedActor,
				UserHost   = resolvedActor.Host,
				TargetUser = await userResolver.ResolveAsync(targetActor.Id, EnforceUriFlags)
			},
			ASNote targetNote => new Bite
			{
				Id         = IdHelpers.GenerateSnowflakeId(activity.PublishedAt),
				CreatedAt  = activity.PublishedAt ?? DateTime.UtcNow,
				Uri        = activity.Id,
				User       = resolvedActor,
				UserHost   = resolvedActor.Host,
				TargetNote = await noteSvc.ResolveNoteAsync(targetNote.Id, user: inboxUser)
			},
			ASBite targetBite => new Bite
			{
				Id        = IdHelpers.GenerateSnowflakeId(activity.PublishedAt),
				CreatedAt = activity.PublishedAt ?? DateTime.UtcNow,
				Uri       = activity.Id,
				User      = resolvedActor,
				UserHost  = resolvedActor.Host,
				TargetBite =
					await db.Bites.FirstAsync(p => p.UserHost == null &&
					                               p.Id == Bite.GetIdFromPublicUri(targetBite.Id, config.Value))
			},
			null => throw GracefulException.UnprocessableEntity($"Failed to resolve bite target {activity.Target.Id}"),
			_ when activity.To?.Id != null => new Bite
			{
				Id         = IdHelpers.GenerateSnowflakeId(activity.PublishedAt),
				CreatedAt  = activity.PublishedAt ?? DateTime.UtcNow,
				Uri        = activity.Id,
				User       = resolvedActor,
				UserHost   = resolvedActor.Host,
				TargetUser = await userResolver.ResolveAsync(activity.To.Id, EnforceUriFlags)
			},
			_ => throw GracefulException.UnprocessableEntity($"Invalid bite target {target.Id} with type {target.Type}")

			//TODO: more fallback
		};

		if ((dbBite.TargetUser?.IsRemoteUser ?? false) ||
		    (dbBite.TargetNote?.User.IsRemoteUser ?? false) ||
		    (dbBite.TargetBite?.User.IsRemoteUser ?? false))
			throw GracefulException.Accepted("Ignoring bite for remote user");

		var finalTarget = dbBite.TargetUser ?? dbBite.TargetNote?.User ?? dbBite.TargetBite?.User;

		if (await db.Blockings.AnyAsync(p => p.Blockee == resolvedActor && p.Blocker == finalTarget))
			throw GracefulException.Forbidden("You are not allowed to interact with this user");

		await db.AddAsync(dbBite);
		await db.SaveChangesAsync();
		await notificationSvc.GenerateBiteNotification(dbBite);
	}

	private async Task HandleAnnounce(ASAnnounce activity, User resolvedActor)
	{
		if (activity.Object == null)
			throw GracefulException.UnprocessableEntity("Announce activity object was null");

		activity.Object = await objectResolver.ResolveObject(activity.Object, resolvedActor.Uri);
		if (activity.Object is not ASNote note)
		{
			logger.LogDebug("Announce activity object is unknown, skipping");
			return;
		}

		var dbNote = await noteSvc.ResolveNoteAsync(note.Id, note);
		if (dbNote == null)
		{
			logger.LogDebug("Announce activity object is unknown, skipping");
			return;
		}

		if (resolvedActor.IsRelayActor)
			return;

		if (await db.Notes.AnyAsync(p => p.Uri == activity.Id))
		{
			logger.LogDebug("Renote '{id}' already exists, skipping", activity.Id);
			return;
		}

		await noteSvc.CreateNoteAsync(new NoteService.NoteCreationData
		{
			User       = resolvedActor,
			Visibility = activity.GetVisibility(resolvedActor),
			Renote     = dbNote,
			Uri        = activity.Id
		});
	}

	private async Task HandleReact(ASEmojiReact activity, User resolvedActor)
	{
		if (resolvedActor.Host == null)
			throw GracefulException.UnprocessableEntity("Cannot process EmojiReact for local actor");
		if (activity.Object == null)
			throw GracefulException.UnprocessableEntity("EmojiReact activity object was null");

		activity.Object = await objectResolver.ResolveObject(activity.Object, resolvedActor.Uri);
		if (activity.Object is not ASNote note)
		{
			logger.LogDebug("EmojiReact activity object is unknown, skipping");
			return;
		}

		await emojiSvc.ProcessEmojiAsync(activity.Tags?.OfType<ASEmoji>().ToList(), resolvedActor.Host);
		await noteSvc.ReactToNoteAsync(note, resolvedActor, activity.Content);
	}

	private async Task HandleBlock(ASBlock activity, User resolvedActor)
	{
		if (activity.Object == null)
			throw GracefulException.UnprocessableEntity("EmojiReact activity object was null");

		activity.Object = await objectResolver.ResolveObject(activity.Object, resolvedActor.Uri);
		if (activity.Object is not ASActor blockee)
		{
			logger.LogDebug("Block activity object is unknown, skipping");
			return;
		}

		var resolvedBlockee = await userResolver.ResolveAsync(blockee.Id, EnforceUriFlags | ResolveFlags.OnlyExisting);
		if (resolvedBlockee == null)
			throw GracefulException.UnprocessableEntity("Unknown block target");
		if (resolvedBlockee.IsRemoteUser)
			throw GracefulException.UnprocessableEntity("Refusing to process block between two remote users");
		await userSvc.BlockUserAsync(resolvedActor, resolvedBlockee);
	}

	private async Task HandleMove(ASMove activity, User resolvedActor)
	{
		if (activity.Target.Id is null) throw GracefulException.UnprocessableEntity("Move target must have an ID");
		var target = await userResolver.ResolveAsync(activity.Target.Id, EnforceUriFlags);
		var source = await userSvc.UpdateUserAsync(resolvedActor, force: true);
		target = await userSvc.UpdateUserAsync(target, force: true);

		var sourceUri = source.Uri ?? source.GetPublicUri(config.Value.WebDomain);
		var targetUri = target.Uri ?? target.GetPublicUri(config.Value.WebDomain);
		var aliases   = target.AlsoKnownAs ?? [];
		if (!aliases.Contains(sourceUri))
			throw GracefulException.UnprocessableEntity("Refusing to process move activity:" +
			                                            "source uri not listed in target aliases");

		source.MovedToUri = targetUri;
		await db.SaveChangesAsync();
		await userSvc.MoveRelationshipsAsync(source, target, sourceUri, targetUri);
	}

	private async Task UnfollowAsync(ASActor followeeActor, User follower)
	{
		//TODO: send reject? or do we not want to copy that part of the old ap core
		var followee = await userResolver.ResolveAsync(followeeActor.Id, EnforceUriFlags);

		await db.FollowRequests.Where(p => p.Follower == follower && p.Followee == followee).ExecuteDeleteAsync();

		// We don't want to use ExecuteDelete for this one to ensure consistency with following counters
		var followings = await db.Followings.Where(p => p.Follower == follower && p.Followee == followee).ToListAsync();
		if (followings.Count > 0)
		{
			await db.Users.Where(p => p.Id == follower.Id)
			        .ExecuteUpdateAsync(p => p.SetProperty(i => i.FollowingCount,
			                                               i => i.FollowingCount - followings.Count));
			await db.Users.Where(p => p.Id == followee.Id)
			        .ExecuteUpdateAsync(p => p.SetProperty(i => i.FollowersCount,
			                                               i => i.FollowersCount - followings.Count));
			db.RemoveRange(followings);
			await db.SaveChangesAsync();

			_ = followupTaskSvc.ExecuteTask("DecrementInstanceIncomingFollowsCounter", async provider =>
			{
				var bgDb          = provider.GetRequiredService<DatabaseContext>();
				var bgInstanceSvc = provider.GetRequiredService<InstanceService>();
				var dbInstance    = await bgInstanceSvc.GetUpdatedInstanceMetadataAsync(follower);
				await bgDb.Instances.Where(p => p.Id == dbInstance.Id)
				          .ExecuteUpdateAsync(p => p.SetProperty(i => i.IncomingFollows, i => i.IncomingFollows + 1));
			});

			await db.Notifications
			        .Where(p => p.Type == Notification.NotificationType.Follow &&
			                    p.Notifiee == followee &&
			                    p.Notifier == follower)
			        .ExecuteDeleteAsync();

			eventSvc.RaiseUserUnfollowed(this, follower, followee);
		}
	}

	private async Task UnblockAsync(User blocker, ASActor blockee)
	{
		var resolvedBlockee = await userResolver.ResolveAsync(blockee.Id, EnforceUriFlags | ResolveFlags.OnlyExisting);
		if (resolvedBlockee == null)
			throw GracefulException.UnprocessableEntity("Unknown block target");
		if (resolvedBlockee.IsRemoteUser)
			throw GracefulException
				.UnprocessableEntity("Refusing to process unblock between two remote users");
		await userSvc.UnblockUserAsync(blocker, resolvedBlockee);
	}
}