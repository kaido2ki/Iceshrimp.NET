using Iceshrimp.Backend.Controllers.Mastodon.Schemas.Entities;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Backend.Core.Services;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Controllers.Mastodon.Renderers;

public class NotificationRenderer(DatabaseContext db, NoteRenderer noteRenderer, UserRenderer userRenderer)
{
	public async Task<NotificationEntity> RenderAsync(
		Notification notification, User user, bool isPleroma, List<AccountEntity>? accounts = null,
		IEnumerable<StatusEntity>? statuses = null
	)
	{
		var dbNotifier = notification.Notifier ?? throw new GracefulException("Notification has no notifier");

		var targetNote = notification.Note;

		var note = targetNote != null
			? statuses?.FirstOrDefault(p => p.Id == targetNote.Id) ??
			  await noteRenderer.RenderAsync(targetNote, user, Filter.FilterContext.Notifications,
			                                 new NoteRenderer.NoteRendererDto { Accounts = accounts })
			: null;

		var notifier = accounts?.FirstOrDefault(p => p.Id == dbNotifier.Id) ??
		               await userRenderer.RenderAsync(dbNotifier);

		string? emojiUrl = null;
		if (notification.Reaction != null && EmojiService.IsCustomEmoji(notification.Reaction)) {
			var parts = notification.Reaction.Trim(':').Split('@');
			emojiUrl = await db.Emojis.Where(e => e.Name == parts[0] && e.Host == (parts.Length > 1 ? parts[1] : null)).Select(e => e.PublicUrl).FirstOrDefaultAsync();
		}

		var res = new NotificationEntity
		{
			Id        = notification.MastoId.ToString(),
			Type      = NotificationEntity.EncodeType(notification.Type, isPleroma),
			Note      = note,
			Notifier  = notifier,
			CreatedAt = notification.CreatedAt.ToStringIso8601Like(),

			Emoji = notification.Reaction,
			EmojiUrl = emojiUrl,
			
			Pleroma = new() {
				// TODO: stub
				IsMuted = false,
				IsSeen = notification.IsRead
			}
		};

		return res;
	}

	public async Task<IEnumerable<NotificationEntity>> RenderManyAsync(
		IEnumerable<Notification> notifications, User user, bool isPleroma
	)
	{
		var notificationList = notifications.ToList();
		if (notificationList.Count == 0) return [];

		var accounts = await noteRenderer.GetAccounts(notificationList.Where(p => p.Notifier != null)
		                                                              .Select(p => p.Notifier)
		                                                              .Concat(notificationList.Select(p => p.Notifiee))
		                                                              .Concat(notificationList
		                                                                      .Select(p => p.Note?.Renote?.User)
		                                                                      .Where(p => p != null))
		                                                              .Cast<User>()
		                                                              .DistinctBy(p => p.Id)
		                                                              .ToList());

		var notes = await noteRenderer.RenderManyAsync(notificationList.Where(p => p.Note != null)
		                                                               .Select(p => p.Note)
		                                                               .Concat(notificationList
		                                                                       .Select(p => p.Note?.Renote)
		                                                                       .Where(p => p != null))
		                                                               .Cast<Note>()
		                                                               .DistinctBy(p => p.Id),
		                                               user, Filter.FilterContext.Notifications, accounts);

		return await notificationList
		             .Select(p => RenderAsync(p, user, isPleroma, accounts, notes))
		             .AwaitAllAsync();
	}
}