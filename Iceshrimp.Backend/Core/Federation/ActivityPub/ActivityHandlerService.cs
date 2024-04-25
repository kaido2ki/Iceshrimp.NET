using System.Diagnostics.CodeAnalysis;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;
using Iceshrimp.Backend.Core.Helpers;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Backend.Core.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

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
	ObjectResolver resolver,
	NotificationService notificationSvc,
	ObjectResolver objectResolver,
	FollowupTaskService followupTaskSvc,
	EmojiService emojiSvc,
	EventService eventSvc
)
{
	public async Task PerformActivityAsync(ASActivity activity, string? inboxUserId, string? authenticatedUserId)
	{
		logger.LogDebug("Processing activity: {activity}", activity.Id);
		if (activity.Actor == null)
			throw GracefulException.UnprocessableEntity("Cannot perform activity as actor 'null'");
		if (await federationCtrl.ShouldBlockAsync(activity.Actor.Id))
			throw GracefulException.UnprocessableEntity("Instance is blocked");
		if (activity.Object == null && activity is not ASBite)
			throw GracefulException.UnprocessableEntity("Activity object is null");

		var resolvedActor = await userResolver.ResolveAsync(activity.Actor.Id);
		if (authenticatedUserId == null)
			throw GracefulException
				.UnprocessableEntity("Refusing to process activity without authFetchUserId");
		if (resolvedActor.Id != authenticatedUserId && authenticatedUserId != null)
			throw GracefulException
				.UnprocessableEntity($"Authorized fetch user id {authenticatedUserId} doesn't match resolved actor id {resolvedActor.Id}");
		if (new Uri(activity.Actor.Id).Host != new Uri(activity.Id).Host)
			throw GracefulException
				.UnprocessableEntity($"Activity identifier ({activity.Actor.Id}) host doesn't match actor identifier ({activity.Id}) host");
		if (resolvedActor.Host == null || resolvedActor.Uri == null)
			throw new Exception("resolvedActor.Host and resolvedActor.Uri must not be null at this stage");

		UpdateInstanceMetadataInBackground(resolvedActor.Host, new Uri(resolvedActor.Uri).Host);

		// Resolve object & children
		if (activity.Object != null)
			activity.Object = await resolver.ResolveObject(activity.Object) ??
			                  throw GracefulException.UnprocessableEntity("Failed to resolve activity object");

		//TODO: validate inboxUserId

		switch (activity)
		{
			case ASCreate:
			{
				//TODO: should we handle other types of creates?
				if (activity.Object is not ASNote note)
					throw GracefulException.UnprocessableEntity("Create activity object is invalid");
				await noteSvc.ProcessNoteAsync(note, resolvedActor);
				return;
			}
			case ASDelete:
			{
				if (activity.Object is ASActor actor)
				{
					if (!await db.Users.AnyAsync(p => p.Uri == actor.Id))
						return;

					if (activity.Actor.Id != actor.Id)
						throw GracefulException.UnprocessableEntity("Refusing to delete user: actor doesn't match");

					await userSvc.DeleteUserAsync(actor);
					return;
				}

				if (activity.Object is not ASTombstone tombstone)
					throw GracefulException
						.UnprocessableEntity($"Delete activity object is invalid: {activity.Object.Type}");
				if (await db.Notes.AnyAsync(p => p.Uri == tombstone.Id))
				{
					await noteSvc.DeleteNoteAsync(tombstone, resolvedActor);
					return;
				}

				if (await db.Users.AnyAsync(p => p.Uri == tombstone.Id))
				{
					if (tombstone.Id != activity.Actor.Id)
						throw GracefulException.UnprocessableEntity("Refusing to delete user: actor doesn't match");

					await userSvc.DeleteUserAsync(activity.Actor);
				}

				logger.LogDebug("Delete activity object {id} is unknown, skipping", tombstone.Id);
				return;
			}
			case ASFollow:
			{
				if (activity.Object is not ASActor obj)
					throw GracefulException.UnprocessableEntity("Follow activity object is invalid");
				var followee = await userResolver.ResolveAsync(obj.Id);
				if (followee.Host != null) throw new Exception("Cannot process follow for remote followee");
				await userSvc.FollowUserAsync(resolvedActor, followee, activity.Id);
				return;
			}
			case ASUnfollow:
			{
				if (activity.Object is not ASActor obj)
					throw GracefulException.UnprocessableEntity("Unfollow activity object is invalid");
				await UnfollowAsync(obj, resolvedActor);
				return;
			}
			case ASAccept:
			{
				if (activity.Object is not ASFollow obj)
					throw GracefulException.UnprocessableEntity("Accept activity object is invalid");
				await AcceptAsync(obj, resolvedActor);
				return;
			}
			case ASReject:
			{
				if (activity.Object is not ASFollow obj)
					throw GracefulException.UnprocessableEntity("Reject activity object is invalid");
				await RejectAsync(obj, resolvedActor);
				return;
			}
			case ASUndo:
			{
				switch (activity.Object)
				{
					case ASFollow { Object: ASActor followee }:
						await UnfollowAsync(followee, resolvedActor);
						return;
					case ASLike { Object: ASNote note } like:
						if (like.MisskeyReaction != null)
							await noteSvc.RemoveReactionFromNoteAsync(note, resolvedActor, like.MisskeyReaction);
						else
							await noteSvc.UnlikeNoteAsync(note, resolvedActor);
						return;
					case ASAnnounce { Object: ASNote note }:
						await noteSvc.UndoAnnounceAsync(note, resolvedActor);
						return;
					case ASEmojiReact { Object: ASNote note } react:
						await noteSvc.RemoveReactionFromNoteAsync(note, resolvedActor, react.Content);
						return;
					case ASBlock { Object: ASActor blockee }:
						await UnblockAsync(resolvedActor, blockee);
						return;
					default:
						throw GracefulException
							.UnprocessableEntity($"Undo activity object is invalid: {activity.Object?.Type}");
				}
			}
			case ASLike like:
			{
				if (activity.Object is not ASNote note)
					throw GracefulException.UnprocessableEntity("Like activity object is invalid");

				if (like.MisskeyReaction != null)
				{
					await emojiSvc.ProcessEmojiAsync(like.Tags?.OfType<ASEmoji>().ToList(), resolvedActor.Host);
					await noteSvc.ReactToNoteAsync(note, resolvedActor, like.MisskeyReaction);
				}
				else
				{
					await noteSvc.LikeNoteAsync(note, resolvedActor);
				}

				return;
			}
			case ASUpdate:
			{
				switch (activity.Object)
				{
					case ASActor actor:
						if (actor.Id != activity.Actor.Id)
							throw GracefulException.UnprocessableEntity("Refusing to update actor with mismatching id");
						await userSvc.UpdateUserAsync(resolvedActor, actor);
						return;
					case ASNote note:
						await noteSvc.ProcessNoteUpdateAsync(note, resolvedActor);
						return;
					default:
						throw GracefulException.UnprocessableEntity("Update activity object is invalid");
				}
			}
			case ASBite bite:
			{
				var target = await objectResolver.ResolveObject(bite.Target);
				var dbBite = target switch
				{
					ASActor targetActor => new Bite
					{
						Id         = IdHelpers.GenerateSlowflakeId(bite.PublishedAt),
						CreatedAt  = bite.PublishedAt ?? DateTime.UtcNow,
						Uri        = bite.Id,
						User       = resolvedActor,
						UserHost   = resolvedActor.Host,
						TargetUser = await userResolver.ResolveAsync(targetActor.Id)
					},
					ASNote targetNote => new Bite
					{
						Id         = IdHelpers.GenerateSlowflakeId(bite.PublishedAt),
						CreatedAt  = bite.PublishedAt ?? DateTime.UtcNow,
						Uri        = bite.Id,
						User       = resolvedActor,
						UserHost   = resolvedActor.Host,
						TargetNote = await noteSvc.ResolveNoteAsync(targetNote.Id)
					},
					ASBite targetBite => new Bite
					{
						Id        = IdHelpers.GenerateSlowflakeId(bite.PublishedAt),
						CreatedAt = bite.PublishedAt ?? DateTime.UtcNow,
						Uri       = bite.Id,
						User      = resolvedActor,
						UserHost  = resolvedActor.Host,
						TargetBite =
							await db.Bites.FirstAsync(p => p.UserHost == null &&
							                               p.Id == Bite.GetIdFromPublicUri(targetBite.Id, config.Value))
					},
					null => throw
						GracefulException.UnprocessableEntity($"Failed to resolve bite target {bite.Target.Id}"),
					_ when bite.To?.Id != null => new Bite
					{
						Id         = IdHelpers.GenerateSlowflakeId(bite.PublishedAt),
						CreatedAt  = bite.PublishedAt ?? DateTime.UtcNow,
						Uri        = bite.Id,
						User       = resolvedActor,
						UserHost   = resolvedActor.Host,
						TargetUser = await userResolver.ResolveAsync(bite.To.Id)
					},
					_ => throw GracefulException
						.UnprocessableEntity($"Invalid bite target {target.Id} with type {target.Type}")

					//TODO: more fallback
				};

				if (dbBite.TargetUser?.Host != null ||
				    dbBite.TargetNote?.User.Host != null ||
				    dbBite.TargetBite?.User.Host != null)
					throw GracefulException.Accepted("Ignoring bite for remote user");

				var finalTarget = dbBite.TargetUser ?? dbBite.TargetNote?.User ?? dbBite.TargetBite?.User;

				if (await db.Blockings.AnyAsync(p => p.Blockee == resolvedActor && p.Blocker == finalTarget))
					throw GracefulException.Forbidden("You are not allowed to interact with this user");

				await db.AddAsync(dbBite);
				await db.SaveChangesAsync();
				await notificationSvc.GenerateBiteNotification(dbBite);
				return;
			}
			case ASAnnounce announce:
			{
				if (announce.Object is not ASNote note)
					throw GracefulException.UnprocessableEntity("Invalid or unsupported announce object");

				var dbNote = await noteSvc.ResolveNoteAsync(note.Id, note);
				await noteSvc.CreateNoteAsync(resolvedActor, announce.GetVisibility(activity.Actor), renote: dbNote,
				                              uri: announce.Id);
				return;
			}
			case ASEmojiReact reaction:
			{
				if (reaction.Object is not ASNote note)
					throw GracefulException.UnprocessableEntity("Invalid or unsupported reaction target");
				await emojiSvc.ProcessEmojiAsync(reaction.Tags?.OfType<ASEmoji>().ToList(), resolvedActor.Host);
				await noteSvc.ReactToNoteAsync(note, resolvedActor, reaction.Content);
				return;
			}
			case ASBlock block:
			{
				if (block.Object is not ASActor blockee)
					throw GracefulException.UnprocessableEntity("Invalid or unsupported block target");
				var resolvedBlockee = await userResolver.ResolveAsync(blockee.Id, true);
				if (resolvedBlockee == null)
					throw GracefulException.UnprocessableEntity("Unknown block target");
				if (resolvedBlockee.Host != null)
					throw GracefulException.UnprocessableEntity("Refusing to process block between two remote users");
				await userSvc.BlockUserAsync(resolvedActor, resolvedBlockee);
				return;
			}
			default:
				throw new NotImplementedException($"Activity type {activity.Type} is unknown");
		}
	}

	private void UpdateInstanceMetadataInBackground(string host, string webDomain)
	{
		_ = followupTaskSvc.ExecuteTask("UpdateInstanceMetadata", async provider =>
		{
			var instanceSvc = provider.GetRequiredService<InstanceService>();
			await instanceSvc.UpdateInstanceStatusAsync(host, webDomain);
		});
	}

	private async Task UnfollowAsync(ASActor followeeActor, User follower)
	{
		//TODO: send reject? or do we not want to copy that part of the old ap core
		var followee = await userResolver.ResolveAsync(followeeActor.Id);

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
		var resolvedBlockee = await userResolver.ResolveAsync(blockee.Id, true);
		if (resolvedBlockee == null)
			throw GracefulException.UnprocessableEntity("Unknown block target");
		if (resolvedBlockee.Host != null)
			throw GracefulException
				.UnprocessableEntity("Refusing to process unblock between two remote users");
		await userSvc.UnblockUserAsync(blocker, resolvedBlockee);
	}

	private async Task AcceptAsync(ASFollow obj, User actor)
	{
		var prefix = $"https://{config.Value.WebDomain}/follows/";
		if (!obj.Id.StartsWith(prefix))
			throw GracefulException.UnprocessableEntity($"Object id '{obj.Id}' not a valid follow request id");

		var ids = obj.Id[prefix.Length..].TrimEnd('/').Split("/");
		if (ids.Length != 2 || ids[1] != actor.Id)
			throw GracefulException
				.UnprocessableEntity($"Actor id '{actor.Id}' doesn't match followee id '{ids[1]}'");

		var request = await db.FollowRequests
		                      .Include(p => p.Follower.UserProfile)
		                      .Include(p => p.Followee.UserProfile)
		                      .FirstOrDefaultAsync(p => p.Followee == actor && p.FollowerId == ids[0]);

		if (request == null)
			throw GracefulException
				.UnprocessableEntity($"No follow request matching follower '{ids[0]}' and followee '{actor.Id}' found");

		await userSvc.AcceptFollowRequestAsync(request);
	}

	private async Task RejectAsync(ASFollow follow, User actor)
	{
		if (follow is not { Actor: not null })
			throw GracefulException.UnprocessableEntity("Refusing to reject object with invalid follow object");

		var resolvedFollower = await userResolver.ResolveAsync(follow.Actor.Id);
		if (resolvedFollower is not { Host: null })
			throw GracefulException.UnprocessableEntity("Refusing to reject remote follow");

		await db.FollowRequests.Where(p => p.Followee == actor && p.Follower == resolvedFollower)
		        .ExecuteDeleteAsync();
		var count = await db.Followings.Where(p => p.Followee == actor && p.Follower == resolvedFollower)
		                    .ExecuteDeleteAsync();
		if (count > 0)
		{
			await db.Users.Where(p => p.Id == resolvedFollower.Id)
			        .ExecuteUpdateAsync(p => p.SetProperty(i => i.FollowingCount, i => i.FollowingCount - count));
			await db.Users.Where(p => p.Id == actor.Id)
			        .ExecuteUpdateAsync(p => p.SetProperty(i => i.FollowersCount, i => i.FollowersCount - count));
			await db.SaveChangesAsync();
		}

		await db.Notifications
		        .Where(p => p.Type == Notification.NotificationType.FollowRequestAccepted)
		        .Where(p => p.Notifiee == resolvedFollower &&
		                    p.Notifier == actor)
		        .ExecuteDeleteAsync();

		await db.UserListMembers
		        .Where(p => p.UserList.User == resolvedFollower && p.User == actor)
		        .ExecuteDeleteAsync();
	}
}