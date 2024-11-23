using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Services;
using Iceshrimp.Shared.Schemas.Web;
using static Iceshrimp.Shared.Schemas.Web.NotificationResponse;

namespace Iceshrimp.Backend.Controllers.Web.Renderers;

public class NotificationRenderer(UserRenderer userRenderer, NoteRenderer noteRenderer, EmojiService emojiSvc) : IScopedService
{
	private static NotificationResponse Render(Notification notification, NotificationRendererDto data)
	{
		var user = notification.Notifier != null
			? data.Users?.First(p => p.Id == notification.Notifier.Id) ??
			  throw new Exception("DTO didn't contain the notifier")
			: null;

		var note = notification.Note != null
			? data.Notes?.First(p => p.Id == notification.Note.Id) ??
			  throw new Exception("DTO didn't contain the note")
			: null;

		var bite = notification.Bite != null
			? data.Bites?.First(p => p.Id == notification.Bite.Id) ??
			  throw new Exception("DTO didn't contain the bite")
			: null;

		var reaction = notification.Reaction != null
			? data.Reactions?.First(p => p.Name == notification.Reaction) ??
			  throw new Exception("DTO didn't contain the reaction")
			: null;

		return new NotificationResponse
		{
			Id        = notification.Id,
			Read      = notification.IsRead,
			CreatedAt = notification.CreatedAt.ToStringIso8601Like(),
			User      = user,
			Note      = note,
			Bite      = bite,
			Reaction  = reaction, 
			Type      = RenderType(notification.Type)
		};
	}

	public async Task<NotificationResponse> RenderOne(
		Notification notification, User localUser
	)
	{
		var data = new NotificationRendererDto
		{
			Users = await GetUsersAsync([notification]),
			Notes = await GetNotesAsync([notification], localUser),
			Bites = GetBites([notification]),
			Reactions = await GetReactionsAsync([notification])
		};

		return Render(notification, data);
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

	private async Task<List<UserResponse>> GetUsersAsync(IEnumerable<Notification> notifications)
	{
		var users = notifications.Select(p => p.Notifier).OfType<User>().DistinctBy(p => p.Id);
		return await userRenderer.RenderManyAsync(users).ToListAsync();
	}

	private async Task<List<NoteResponse>> GetNotesAsync(IEnumerable<Notification> notifications, User user)
	{
		var notes = notifications.Select(p => p.Note).OfType<Note>().DistinctBy(p => p.Id);
		return await noteRenderer.RenderManyAsync(notes, user, Filter.FilterContext.Notifications).ToListAsync();
	}

	private static List<BiteResponse> GetBites(IEnumerable<Notification> notifications)
	{
		var bites = notifications.Select(p => p.Bite).NotNull().DistinctBy(p => p.Id);
		return bites.Select(p => new BiteResponse { Id = p.Id, BiteBack = p.TargetBiteId != null }).ToList();
	}
	
	private async Task<List<ReactionResponse>> GetReactionsAsync(IEnumerable<Notification> notifications)
	{
		var reactions = notifications.Select(p => p.Reaction).NotNull().ToList();

		var emojis = reactions.Where(p => !p.StartsWith(':')).Select(p => new ReactionResponse { Name = p, Url = null, Sensitive = false }).ToList();
		var custom  = reactions.Where(p => p.StartsWith(':')).ToAsyncEnumerable();

		await foreach (var s in custom)
		{
			var emoji = await emojiSvc.ResolveEmojiAsync(s);
			var reaction = emoji != null
				? new ReactionResponse { Name = s, Url = emoji.PublicUrl, Sensitive = emoji.Sensitive }
				: new ReactionResponse { Name = s, Url = null, Sensitive = false };
			
			emojis.Add(reaction);
		}
			
		return emojis;
	}

	public async Task<IEnumerable<NotificationResponse>> RenderManyAsync(
		IEnumerable<Notification> notifications, User user
	)
	{
		var notificationsList = notifications.ToList();
		var data = new NotificationRendererDto
		{
			Users = await GetUsersAsync(notificationsList),
			Notes = await GetNotesAsync(notificationsList, user),
			Bites = GetBites(notificationsList),
			Reactions = await GetReactionsAsync(notificationsList)
		};

		return notificationsList.Select(p => Render(p, data));
	}

	private class NotificationRendererDto
	{
		public List<NoteResponse>? Notes;
		public List<UserResponse>? Users;
		public List<BiteResponse>? Bites;
		public List<ReactionResponse>? Reactions;
	}
}