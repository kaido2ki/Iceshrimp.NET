using System.Diagnostics.CodeAnalysis;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Services;

public class NotificationService(
	[SuppressMessage("ReSharper", "SuggestBaseTypeForParameterInConstructor")]
	DatabaseContext db,
	EventService eventSvc
)
{
	public async Task GenerateMentionNotifications(Note note, IReadOnlyCollection<string> mentionedLocalUserIds)
	{
		if (mentionedLocalUserIds.Count == 0) return;

		var blocks = await db.Blockings
		                     .Where(p => p.BlockeeId == note.UserId && mentionedLocalUserIds.Contains(p.BlockerId))
		                     .Select(p => p.BlockerId)
		                     .ToListAsync();

		var notifications = mentionedLocalUserIds
		                    .Where(p => p != note.UserId)
		                    .Except(blocks)
		                    .Select(p => new Notification
		                    {
			                    Id         = IdHelpers.GenerateSlowflakeId(),
			                    CreatedAt  = DateTime.UtcNow,
			                    Note       = note,
			                    NotifierId = note.UserId,
			                    NotifieeId = p,
			                    Type       = Notification.NotificationType.Mention
		                    })
		                    .ToList();

		await db.AddRangeAsync(notifications);
		await db.SaveChangesAsync();
		eventSvc.RaiseNotifications(this, notifications);
	}

	public async Task GenerateReplyNotifications(Note note, IReadOnlyCollection<string> mentionedLocalUserIds)
	{
		if (note.Visibility != Note.NoteVisibility.Specified) return;
		if (note.VisibleUserIds.Count == 0) return;

		var users = mentionedLocalUserIds.Concat(note.VisibleUserIds).Distinct().Except(mentionedLocalUserIds).ToList();
		if (users.Count == 0) return;

		var blocks = await db.Blockings
		                     .Where(p => p.BlockeeId == note.UserId && mentionedLocalUserIds.Contains(p.BlockerId))
		                     .Select(p => p.BlockerId)
		                     .ToListAsync();

		var notifications = users
		                    .Where(p => p != note.UserId)
		                    .Except(blocks)
		                    .Select(p => new Notification
		                    {
			                    Id         = IdHelpers.GenerateSlowflakeId(),
			                    CreatedAt  = DateTime.UtcNow,
			                    Note       = note,
			                    NotifierId = note.UserId,
			                    NotifieeId = p,
			                    Type       = Notification.NotificationType.Reply
		                    })
		                    .ToList();

		await db.AddRangeAsync(notifications);
		await db.SaveChangesAsync();
		eventSvc.RaiseNotifications(this, notifications);
	}

	[SuppressMessage("ReSharper", "EntityFramework.UnsupportedServerSideFunctionCall",
	                 Justification = "Projectable functions are very much translatable")]
	public async Task GenerateEditNotifications(Note note)
	{
		var notifications = await db.Users
		                            .Where(p => p.Host == null && p != note.User && p.HasInteractedWith(note))
		                            .Select(p => new Notification
		                            {
			                            Id         = IdHelpers.GenerateSlowflakeId(DateTime.UtcNow),
			                            CreatedAt  = DateTime.UtcNow,
			                            Note       = note,
			                            NotifierId = note.UserId,
			                            Notifiee   = p,
			                            Type       = Notification.NotificationType.Edit
		                            })
		                            .ToListAsync();

		if (notifications.Count == 0)
			return;

		await db.AddRangeAsync(notifications);
		await db.SaveChangesAsync();
		eventSvc.RaiseNotifications(this, notifications);
	}

	public async Task GenerateLikeNotification(Note note, User user)
	{
		if (note.UserHost != null) return;
		if (note.User == user) return;

		var notification = new Notification
		{
			Id        = IdHelpers.GenerateSlowflakeId(),
			CreatedAt = DateTime.UtcNow,
			Note      = note,
			Notifiee  = note.User,
			Notifier  = user,
			Type      = Notification.NotificationType.Like
		};

		await db.AddAsync(notification);
		await db.SaveChangesAsync();
		eventSvc.RaiseNotification(this, notification);
	}

	public async Task GenerateReactionNotification(NoteReaction reaction)
	{
		if (reaction.Note.User.Host != null) return;
		if (reaction.Note.User == reaction.User) return;

		var notification = new Notification
		{
			Id        = IdHelpers.GenerateSlowflakeId(),
			CreatedAt = DateTime.UtcNow,
			Note      = reaction.Note,
			Notifiee  = reaction.Note.User,
			Notifier  = reaction.User,
			Type      = Notification.NotificationType.Reaction
		};

		await db.AddAsync(notification);
		await db.SaveChangesAsync();
		eventSvc.RaiseNotification(this, notification);
	}

	public async Task GenerateFollowNotification(User follower, User followee)
	{
		if (followee.Host != null) return;

		var notification = new Notification
		{
			Id        = IdHelpers.GenerateSlowflakeId(),
			CreatedAt = DateTime.UtcNow,
			Notifiee  = followee,
			Notifier  = follower,
			Type      = Notification.NotificationType.Follow
		};

		await db.AddAsync(notification);
		await db.SaveChangesAsync();
		eventSvc.RaiseNotification(this, notification);
		eventSvc.RaiseUserFollowed(this, follower, followee);
	}

	public async Task GenerateFollowRequestReceivedNotification(FollowRequest followRequest)
	{
		if (followRequest.FolloweeHost != null) return;

		var notification = new Notification
		{
			Id            = IdHelpers.GenerateSlowflakeId(),
			CreatedAt     = DateTime.UtcNow,
			FollowRequest = followRequest,
			Notifier      = followRequest.Follower,
			Notifiee      = followRequest.Followee,
			Type          = Notification.NotificationType.FollowRequestReceived
		};

		await db.AddAsync(notification);
		await db.SaveChangesAsync();
		eventSvc.RaiseNotification(this, notification);
	}

	public async Task GenerateFollowRequestAcceptedNotification(FollowRequest followRequest)
	{
		if (followRequest.FollowerHost != null) return;
		if (!followRequest.Followee.IsLocked) return;

		var notification = new Notification
		{
			Id            = IdHelpers.GenerateSlowflakeId(),
			CreatedAt     = DateTime.UtcNow,
			Notifier      = followRequest.Followee,
			Notifiee      = followRequest.Follower,
			Type          = Notification.NotificationType.FollowRequestAccepted
		};

		await db.AddAsync(notification);
		await db.SaveChangesAsync();
		eventSvc.RaiseNotification(this, notification);
		eventSvc.RaiseUserFollowed(this, followRequest.Follower, followRequest.Followee);
	}

	public async Task GenerateBiteNotification(Bite bite)
	{
		var notification = new Notification
		{
			Id        = IdHelpers.GenerateSlowflakeId(),
			CreatedAt = DateTime.UtcNow,
			Notifiee = (bite.TargetUser ?? bite.TargetNote?.User ?? bite.TargetBite?.User) ??
			           throw new InvalidOperationException("Null checks say one of these must not be null"),
			Notifier = bite.User,
			Bite     = bite,
			Type     = Notification.NotificationType.Bite
		};

		await db.AddAsync(notification);
		await db.SaveChangesAsync();
		eventSvc.RaiseNotification(this, notification);
	}

	public async Task GeneratePollEndedNotifications(Note note)
	{
		var notifications = await db.PollVotes
		                            .Where(p => p.Note == note)
		                            .Where(p => p.User.Host == null)
		                            .Select(p => p.User)
		                            .Concat(db.Users.Where(p => p == note.User))
		                            .Distinct()
		                            .Select(p => new Notification
		                            {
			                            Id        = IdHelpers.GenerateSlowflakeId(DateTime.UtcNow),
			                            CreatedAt = DateTime.UtcNow,
			                            Notifiee  = p,
			                            Notifier  = note.User,
			                            Note      = note,
			                            Type      = Notification.NotificationType.PollEnded
		                            })
		                            .ToListAsync();

		if (note.UserHost == null && notifications.All(p => p.Notifiee != note.User))
		{
			notifications.Add(new Notification
			{
				Id        = IdHelpers.GenerateSlowflakeId(DateTime.UtcNow),
				CreatedAt = DateTime.UtcNow,
				Notifiee  = note.User,
				Note      = note,
				Type      = Notification.NotificationType.PollEnded
			});
		}

		await db.AddRangeAsync(notifications);
		await db.SaveChangesAsync();

		foreach (var notification in notifications)
			eventSvc.RaiseNotification(this, notification);
	}

	[SuppressMessage("ReSharper", "EntityFramework.UnsupportedServerSideFunctionCall", Justification = "Projectables")]
	public async Task GenerateRenoteNotification(Note note)
	{
		if (note.Renote is not { UserHost: null }) return;
		if (note.RenoteUserId == note.UserId) return;
		if (!note.VisibilityIsPublicOrHome &&
		    !await db.Notes.AnyAsync(p => p.Id == note.Id && p.IsVisibleFor(note.Renote.User)))
			return;

		var notification = new Notification
		{
			Id        = IdHelpers.GenerateSlowflakeId(),
			CreatedAt = DateTime.UtcNow,
			Note      = note.IsQuote ? note : note.Renote,
			Notifiee  = note.Renote.User,
			Notifier  = note.User,
			Type      = note.IsQuote ? Notification.NotificationType.Quote : Notification.NotificationType.Renote
		};

		await db.AddAsync(notification);
		await db.SaveChangesAsync();
		eventSvc.RaiseNotification(this, notification);
	}
}