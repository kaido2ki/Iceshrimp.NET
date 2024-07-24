using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Events;

namespace Iceshrimp.Backend.Core.Services;

public class EventService : IEventService
{
	public event EventHandler<Note>?            NotePublished;
	public event EventHandler<Note>?            NoteUpdated;
	public event EventHandler<Note>?            NoteDeleted;
	public event EventHandler<NoteInteraction>? NoteLiked;
	public event EventHandler<NoteInteraction>? NoteUnliked;
	public event EventHandler<NoteReaction>?    NoteReacted;
	public event EventHandler<NoteReaction>?    NoteUnreacted;
	public event EventHandler<UserInteraction>? UserFollowed;
	public event EventHandler<UserInteraction>? UserUnfollowed;
	public event EventHandler<UserInteraction>? UserBlocked;
	public event EventHandler<UserInteraction>? UserUnblocked;
	public event EventHandler<UserInteraction>? UserMuted;
	public event EventHandler<UserInteraction>? UserUnmuted;
	public event EventHandler<Notification>?    Notification;
	public event EventHandler<Filter>?          FilterAdded;
	public event EventHandler<Filter>?          FilterRemoved;
	public event EventHandler<Filter>?          FilterUpdated;
	public event EventHandler<UserList>?        ListMembersUpdated;

	public Task RaiseNotePublished(object? sender, Note note)
	{
		NotePublished?.Invoke(sender, note);
		return Task.CompletedTask;
	}

	public Task RaiseNoteUpdated(object? sender, Note note)
	{
		NoteUpdated?.Invoke(sender, note);
		return Task.CompletedTask;
	}

	public Task RaiseNoteDeleted(object? sender, Note note)
	{
		NoteDeleted?.Invoke(sender, note);
		return Task.CompletedTask;
	}

	public Task RaiseNotification(object? sender, Notification notification)
	{
		Notification?.Invoke(sender, notification);
		return Task.CompletedTask;
	}

	public Task RaiseNotifications(object? sender, IEnumerable<Notification> notifications)
	{
		foreach (var notification in notifications) Notification?.Invoke(sender, notification);
		return Task.CompletedTask;
	}

	public Task RaiseNoteLiked(object? sender, Note note, User user)
	{
		NoteLiked?.Invoke(sender, new NoteInteraction { Note = note, User = user });
		return Task.CompletedTask;
	}

	public Task RaiseNoteUnliked(object? sender, Note note, User user)
	{
		NoteUnliked?.Invoke(sender, new NoteInteraction { Note = note, User = user });
		return Task.CompletedTask;
	}

	public Task RaiseNoteReacted(object? sender, NoteReaction reaction)
	{
		NoteReacted?.Invoke(sender, reaction);
		return Task.CompletedTask;
	}

	public Task RaiseNoteUnreacted(object? sender, NoteReaction reaction)
	{
		NoteUnreacted?.Invoke(sender, reaction);
		return Task.CompletedTask;
	}

	public Task RaiseUserFollowed(object? sender, User actor, User obj)
	{
		UserFollowed?.Invoke(sender, new UserInteraction { Actor = actor, Object = obj });
		return Task.CompletedTask;
	}

	public Task RaiseUserUnfollowed(object? sender, User actor, User obj)
	{
		UserUnfollowed?.Invoke(sender, new UserInteraction { Actor = actor, Object = obj });
		return Task.CompletedTask;
	}

	public Task RaiseUserBlocked(object? sender, User actor, User obj)
	{
		UserBlocked?.Invoke(sender, new UserInteraction { Actor = actor, Object = obj });
		return Task.CompletedTask;
	}

	public Task RaiseUserUnblocked(object? sender, User actor, User obj)
	{
		UserUnblocked?.Invoke(sender, new UserInteraction { Actor = actor, Object = obj });
		return Task.CompletedTask;
	}

	public Task RaiseUserMuted(object? sender, User actor, User obj)
	{
		UserMuted?.Invoke(sender, new UserInteraction { Actor = actor, Object = obj });
		return Task.CompletedTask;
	}

	public Task RaiseUserUnmuted(object? sender, User actor, User obj)
	{
		UserUnmuted?.Invoke(sender, new UserInteraction { Actor = actor, Object = obj });
		return Task.CompletedTask;
	}

	public Task RaiseFilterAdded(object? sender, Filter filter)
	{
		FilterAdded?.Invoke(sender, filter);
		return Task.CompletedTask;
	}

	public Task RaiseFilterRemoved(object? sender, Filter filter)
	{
		FilterRemoved?.Invoke(sender, filter);
		return Task.CompletedTask;
	}

	public Task RaiseFilterUpdated(object? sender, Filter filter)
	{
		FilterUpdated?.Invoke(sender, filter);
		return Task.CompletedTask;
	}

	public Task RaiseListMembersUpdated(object? sender, UserList list)
	{
		ListMembersUpdated?.Invoke(sender, list);
		return Task.CompletedTask;
	}
}