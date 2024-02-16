using Iceshrimp.Backend.Controllers.Mastodon.Schemas.Entities;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Middleware;

namespace Iceshrimp.Backend.Controllers.Mastodon.Renderers;

public class NotificationRenderer(NoteRenderer noteRenderer, UserRenderer userRenderer) {
	public async Task<NotificationEntity> RenderAsync(
		Notification notification, User? user, List<AccountEntity>? accounts = null, IEnumerable<StatusEntity>? statuses = null
	) {
		var dbNotifier = notification.Notifier ?? throw new GracefulException("Notification has no notifier");

		var note = notification.Note != null
			? statuses?.FirstOrDefault(p => p.Id == notification.Note.Id) ??
			  await noteRenderer.RenderAsync(notification.Note, user, accounts)
			: null;

		var notifier = accounts?.FirstOrDefault(p => p.Id == dbNotifier.Id) ??
		               await userRenderer.RenderAsync(dbNotifier);

		//TODO: specially handle quotes

		var res = new NotificationEntity {
			Id        = notification.Id,
			Type      = NotificationEntity.EncodeType(notification.Type),
			Note      = note,
			Notifier  = notifier,
			CreatedAt = notification.CreatedAt.ToStringMastodon()
		};

		return res;
	}

	public async Task<IEnumerable<NotificationEntity>> RenderManyAsync(
		IEnumerable<Notification> notifications, User? user
	) {
		var notificationList = notifications.ToList();

		var accounts = await noteRenderer.GetAccounts(notificationList.Where(p => p.Notifier != null)
		                                                              .Select(p => p.Notifier)
		                                                              .Concat(notificationList.Select(p => p.Notifiee))
		                                                              .Cast<User>()
		                                                              .DistinctBy(p => p.Id));

		var notes = await noteRenderer.RenderManyAsync(notificationList.Where(p => p.Note != null)
		                                                               .Select(p => p.Note)
		                                                               .Cast<Note>()
		                                                               .DistinctBy(p => p.Id), user, accounts);

		return await notificationList
		             .Select(p => RenderAsync(p, user, accounts, notes))
		             .AwaitAllAsync();
	}
}