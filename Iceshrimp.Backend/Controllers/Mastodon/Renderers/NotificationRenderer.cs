using Iceshrimp.Backend.Controllers.Mastodon.Schemas.Entities;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Middleware;
using Notification = Iceshrimp.Backend.Controllers.Mastodon.Schemas.Entities.Notification;
using DbNotification = Iceshrimp.Backend.Core.Database.Tables.Notification;

namespace Iceshrimp.Backend.Controllers.Mastodon.Renderers;

public class NotificationRenderer(NoteRenderer noteRenderer, UserRenderer userRenderer) {
	public async Task<Notification> RenderAsync(
		DbNotification notification, List<Account>? accounts = null, IEnumerable<Status>? statuses = null
	) {
		var dbNotifier = notification.Notifier ?? throw new GracefulException("Notification has no notifier");

		var note = notification.Note != null
			? statuses?.FirstOrDefault(p => p.Id == notification.Note.Id) ??
			  await noteRenderer.RenderAsync(notification.Note, accounts)
			: null;

		var notifier = accounts?.FirstOrDefault(p => p.Id == dbNotifier.Id) ??
		               await userRenderer.RenderAsync(dbNotifier);
		
		//TODO: specially handle quotes

		var res = new Notification {
			Id        = notification.Id,
			Type      = Notification.EncodeType(notification.Type),
			Note      = note,
			Notifier  = notifier,
			CreatedAt = notification.CreatedAt.ToStringMastodon()
		};

		return res;
	}

	public async Task<IEnumerable<Notification>> RenderManyAsync(IEnumerable<DbNotification> notifications) {
		var notificationList = notifications.ToList();

		var accounts = await noteRenderer.GetAccounts(notificationList.Where(p => p.Notifier != null)
		                                                              .Select(p => p.Notifier)
		                                                              .Concat(notificationList.Select(p => p.Notifiee))
		                                                              .Cast<User>()
		                                                              .DistinctBy(p => p.Id));

		var notes = await noteRenderer.RenderManyAsync(notificationList.Where(p => p.Note != null)
		                                                               .Select(p => p.Note)
		                                                               .Cast<Note>()
		                                                               .DistinctBy(p => p.Id), accounts);

		return await notificationList
		             .Select(p => RenderAsync(p, accounts, notes))
		             .AwaitAllAsync();
	}
}