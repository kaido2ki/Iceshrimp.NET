using System.Diagnostics.CodeAnalysis;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Helpers;

namespace Iceshrimp.Backend.Core.Services;

public class NotificationService(
	[SuppressMessage("ReSharper", "SuggestBaseTypeForParameterInConstructor")]
	DatabaseContext db
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
		                    });

		await db.AddRangeAsync(notifications);
		await db.SaveChangesAsync();
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
		                    });

		await db.AddRangeAsync(notifications);
		await db.SaveChangesAsync();
	}
}