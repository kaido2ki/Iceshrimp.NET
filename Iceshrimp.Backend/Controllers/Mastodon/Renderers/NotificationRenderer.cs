using Iceshrimp.Backend.Controllers.Mastodon.Schemas.Entities;
using Iceshrimp.Backend.Controllers.Pleroma.Schemas.Entities;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Services;
using Iceshrimp.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Controllers.Mastodon.Renderers;

public class NotificationRenderer(
	IOptions<Config.InstanceSection> instance,
	DatabaseContext db,
	NoteRenderer noteRenderer,
	UserRenderer userRenderer,
	FlagService flags
) : IScopedService
{
	public async Task<NotificationEntity> RenderAsync(
		Notification notification, User user, bool isPleroma, List<AccountEntity>? accounts = null,
		IEnumerable<StatusEntity>? statuses = null, Dictionary<string, string>? emojiUrls = null,
		bool bittenBack = false
	)
	{
		var dbNotifier = notification.Notifier ?? throw new Exception("Notification has no notifier");

		var targetNote = notification.Note;

		var note = targetNote != null
			? statuses?.FirstOrDefault(p => p.Id == targetNote.Id)
			  ?? await noteRenderer.RenderAsync(targetNote, user, Filter.FilterContext.Notifications,
			                                    new NoteRenderer.NoteRendererDto { Accounts = accounts })
			: null;

		var notifier = accounts?.FirstOrDefault(p => p.Id == dbNotifier.Id)
		               ?? await userRenderer.RenderAsync(dbNotifier, user);

		string? emojiUrl = null;
		if (notification.Reaction != null)
		{
			// explicitly check to skip another database call if url is actually null
			if (emojiUrls != null)
			{
				emojiUrl = emojiUrls.GetValueOrDefault(notification.Reaction);
			}
			else if (EmojiService.IsCustomEmoji(notification.Reaction))
			{
				var parts = notification.Reaction.Trim(':').Split('@');
				emojiUrl = await db.Emojis
				                   .Where(e => e.Name == parts[0] && e.Host == (parts.Length > 1 ? parts[1] : null))
				                   .Select(e => e.GetAccessUrl(instance.Value))
				                   .FirstOrDefaultAsync();
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
			Bite      = notification.BiteId != null
				? new BiteEntity { Id = notification.BiteId, BiteBack = bittenBack }
				: null,
			Pleroma = flags.IsPleroma.Value
				? new PleromaNotificationExtensions { IsSeen = notification.IsRead }
				: null
		};

		return res;
	}

	public async Task<IEnumerable<NotificationEntity>> RenderManyAsync(
		IEnumerable<Notification> notifications, User user, bool isPleroma
	)
	{
		var notificationList = notifications.ToList();
		if (notificationList.Count == 0) return [];

		var accounts = await noteRenderer
			.GetAccountsAsync(notificationList
			                  .Where(p => p.Notifier != null)
			                  .Select(p => p.Notifier)
			                  .Concat(notificationList.Select(p => p.Notifiee))
			                  .Concat(notificationList.Select(p => p.Note?.Renote?.User).Where(p => p != null))
			                  .Cast<User>()
			                  .DistinctBy(p => p.Id)
			                  .ToList(), user);

		var notes = await noteRenderer
			.RenderManyAsync(notificationList.Where(p => p.Note != null)
			                                 .Select(p => p.Note)
			                                 .Concat(notificationList.Select(p => p.Note?.Renote).Where(p => p != null))
			                                 .Cast<Note>()
			                                 .DistinctBy(p => p.Id),
			                 user, Filter.FilterContext.Notifications, accounts);

		var parts = notificationList.Where(p => p.Reaction != null && EmojiService.IsCustomEmoji(p.Reaction))
		                            .Select(p =>
		                            {
			                            var parts = p.Reaction!.Trim(':').Split('@');
			                            return (name: parts[0], host: parts.Length > 1 ? parts[1] : null);
		                            })
		                            .Distinct()
		                            .ToArray();

		var expr = ExpressionExtensions.False<Emoji>();
		expr = parts.Aggregate(expr, (current, part) => current.Or(p => p.Name == part.name && p.Host == part.host));

		// https://github.com/dotnet/efcore/issues/31492
		var emojiUrls = await db.Emojis
		                        .Where(expr)
		                        .Select(e => new
		                        {
			                        Name = $":{e.Name}{(e.Host != null ? "@" + e.Host : "")}:",
			                        Url  = e.GetAccessUrl(instance.Value)
		                        })
		                        .ToDictionaryAsync(e => e.Name, e => e.Url);

		var bites = notificationList
		            .Where(p => p.Type == Notification.NotificationType.Bite)
		            .Select(p => p.BiteId)
		            .ToList();

		List<string>? bittenBack = null;
		if (bites.Count > 0) bittenBack = await db.Bites
		                              .Where(p => p.TargetBiteId != null && bites.Contains(p.Id))
		                              .Select(p => p.Id)
		                              .ToListAsync();

		var res = await notificationList
		                .Select(p => RenderAsync(p, user, isPleroma, accounts, notes, emojiUrls, bittenBack?.FirstOrDefault(b => b == p.BiteId) != null))
		                .AwaitAllAsync();

		return res;
	}
}
