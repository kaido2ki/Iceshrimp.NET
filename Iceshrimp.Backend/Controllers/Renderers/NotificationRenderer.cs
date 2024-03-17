using Iceshrimp.Backend.Controllers.Schemas;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;

namespace Iceshrimp.Backend.Controllers.Renderers;

public class NotificationRenderer(
	UserRenderer userRenderer,
	NoteRenderer noteRenderer
)
{
	public async Task<NotificationResponse> RenderOne(
		Notification notification, User localUser, NotificationRendererDto? data = null
	)
	{
		var user = notification.Notifier != null
			? (data?.Users ?? await GetUsers([notification])).First(p => p.Id == notification.Notifier.Id)
			: null;

		var note = notification.Note != null
			? (data?.Notes ?? await GetNotes([notification], localUser)).First(p => p.Id == notification.Note.Id)
			: null;

		return new NotificationResponse
		{
			Id        = notification.Id,
			Read      = notification.IsRead,
			CreatedAt = notification.CreatedAt.ToStringIso8601Like(),
			User      = user,
			Note      = note,
			Type      = RenderType(notification.Type)
		};
	}

	private static string RenderType(Notification.NotificationType type) => type switch
	{
		Notification.NotificationType.Follow                => "follow",
		Notification.NotificationType.Mention               => "mention",
		Notification.NotificationType.Reply                 => "reply",
		Notification.NotificationType.Renote                => "renote",
		Notification.NotificationType.Quote                 => "quote",
		Notification.NotificationType.Like                  => "like",
		Notification.NotificationType.Reaction              => "reaction",
		Notification.NotificationType.PollVote              => "pollVote",
		Notification.NotificationType.PollEnded             => "pollEnded",
		Notification.NotificationType.FollowRequestReceived => "followRequestReceived",
		Notification.NotificationType.FollowRequestAccepted => "followRequestAccepted",
		Notification.NotificationType.GroupInvited          => "groupInvited",
		Notification.NotificationType.App                   => "app",
		Notification.NotificationType.Edit                  => "edit",
		Notification.NotificationType.Bite                  => "bite",

		_ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
	};

	private async Task<List<UserResponse>> GetUsers(IEnumerable<Notification> notifications)
	{
		var users = notifications.Select(p => p.Notifier).OfType<User>().DistinctBy(p => p.Id);
		return await userRenderer.RenderMany(users).ToListAsync();
	}

	private async Task<List<NoteResponse>> GetNotes(IEnumerable<Notification> notifications, User user)
	{
		var notes = notifications.Select(p => p.Note).OfType<Note>().DistinctBy(p => p.Id);
		return await noteRenderer.RenderMany(notes, user).ToListAsync();
	}

	public async Task<IEnumerable<NotificationResponse>> RenderMany(IEnumerable<Notification> notifications, User user)
	{
		var notificationsList = notifications.ToList();
		var data = new NotificationRendererDto
		{
			Users = await GetUsers(notificationsList), Notes = await GetNotes(notificationsList, user)
		};

		return await notificationsList.Select(p => RenderOne(p, user, data)).AwaitAllAsync();
	}

	public class NotificationRendererDto
	{
		public List<NoteResponse>? Notes;
		public List<UserResponse>? Users;
	}
}