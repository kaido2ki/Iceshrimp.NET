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
		IEnumerable<StatusEntity>? statuses = null, Dictionary<string, string>? emojiUrls = null
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
		if (notification.Reaction != null) {
			// explicitly check to skip another database call if url is actually null
			if (emojiUrls != null)
			{
				emojiUrl = emojiUrls.GetValueOrDefault(notification.Reaction);
			}
			else if (EmojiService.IsCustomEmoji(notification.Reaction))
			{
				var parts = notification.Reaction.Trim(':').Split('@');
				emojiUrl = await db.Emojis.Where(e => e.Name == parts[0] && e.Host == (parts.Length > 1 ? parts[1] : null)).Select(e => e.PublicUrl).FirstOrDefaultAsync();
			}
		}

		var res = new NotificationEntity
		{
			Id        = notification.MastoId.ToString(),
			Type      = NotificationEntity.EncodeType(notification.Type, isPleroma),
			Note      = note,
			Notifier  = notifier,
			CreatedAt = notification.CreatedAt.ToStringIso8601Like(),
			Emoji     = notification.Reaction,
			EmojiUrl  = emojiUrl,
			Pleroma   = new() {
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

		var parts = notifications.Where(p => p.Reaction != null && EmojiService.IsCustomEmoji(p.Reaction)).Select(p => {
			var parts = p.Reaction!.Trim(':').Split('@');
			return new { Name = parts[0], Host = parts.Length > 1 ? parts[1] : null };
		});

		// https://github.com/dotnet/efcore/issues/31492
		IQueryable<Emoji> urlQ = db.Emojis;
		foreach (var part in parts)
			urlQ = urlQ.Concat(db.Emojis.Where(e => e.Name == part.Name && e.Host == part.Host));
		var emojiUrls = (await urlQ
				.Select(e => new { Name = $":{e.Name}{(e.Host != null ? "@" + e.Host : "")}:", Url = e.PublicUrl })
				.ToArrayAsync())
			.DistinctBy(e => e.Name)
			.ToDictionary(e => e.Name, e => e.Url);

		return await notificationList
		             .Select(p => RenderAsync(p, user, isPleroma, accounts, notes, emojiUrls))
		             .AwaitAllAsync();
	}
}