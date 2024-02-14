using System.Diagnostics.CodeAnalysis;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Helpers;

namespace Iceshrimp.Backend.Core.Services;

public class NotificationService(
	[SuppressMessage("ReSharper", "SuggestBaseTypeForParameterInConstructor")]
	DatabaseContext db,
	EventService eventSvc
) {
	public async Task GenerateMentionNotifications(Note note, IReadOnlyCollection<string> mentionedLocalUserIds) {
		if (mentionedLocalUserIds.Count == 0) return;

		var notifications = mentionedLocalUserIds
		                    .Where(p => p != note.UserId)
		                    .Select(p => new Notification {
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

	public async Task GenerateReplyNotifications(Note note, IReadOnlyCollection<string> mentionedLocalUserIds) {
		if (note.Visibility != Note.NoteVisibility.Specified) return;
		if (note.VisibleUserIds.Count == 0) return;

		var users = mentionedLocalUserIds.Concat(note.VisibleUserIds).Distinct().Except(mentionedLocalUserIds).ToList();
		if (users.Count == 0) return;

		var notifications = users
		                    .Where(p => p != note.UserId)
		                    .Select(p => new Notification {
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

	public async Task GenerateLikeNotification(Note note, User user) {
		if (note.UserHost != null) return;
		if (note.User == user) return;

		var notification = new Notification {
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
}