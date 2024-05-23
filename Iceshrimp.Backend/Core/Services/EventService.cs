using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Events;

namespace Iceshrimp.Backend.Core.Services;

public class EventService
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

	public void RaiseNotePublished(object? sender, Note note) => NotePublished?.Invoke(sender, note);
	public void RaiseNoteUpdated(object? sender, Note note)   => NoteUpdated?.Invoke(sender, note);
	public void RaiseNoteDeleted(object? sender, Note note)   => NoteDeleted?.Invoke(sender, note);

	public void RaiseNotification(object? sender, Notification notification) =>
		Notification?.Invoke(sender, notification);

	public void RaiseNotifications(object? sender, IEnumerable<Notification> notifications)
	{
		foreach (var notification in notifications) Notification?.Invoke(sender, notification);
	}

	public void RaiseNoteLiked(object? sender, Note note, User user) =>
		NoteLiked?.Invoke(sender, new NoteInteraction { Note = note, User = user });

	public void RaiseNoteUnliked(object? sender, Note note, User user) =>
		NoteUnliked?.Invoke(sender, new NoteInteraction { Note = note, User = user });

	public void RaiseNoteReacted(object? sender, NoteReaction reaction) =>
		NoteReacted?.Invoke(sender, reaction);

	public void RaiseNoteUnreacted(object? sender, NoteReaction reaction) =>
		NoteUnreacted?.Invoke(sender, reaction);

	public void RaiseUserFollowed(object? sender, User actor, User obj) =>
		UserFollowed?.Invoke(sender, new UserInteraction { Actor = actor, Object = obj });

	public void RaiseUserUnfollowed(object? sender, User actor, User obj) =>
		UserUnfollowed?.Invoke(sender, new UserInteraction { Actor = actor, Object = obj });

	public void RaiseUserBlocked(object? sender, User actor, User obj) =>
		UserBlocked?.Invoke(sender, new UserInteraction { Actor = actor, Object = obj });

	public void RaiseUserUnblocked(object? sender, User actor, User obj) =>
		UserUnblocked?.Invoke(sender, new UserInteraction { Actor = actor, Object = obj });

	public void RaiseUserMuted(object? sender, User actor, User obj) =>
		UserMuted?.Invoke(sender, new UserInteraction { Actor = actor, Object = obj });

	public void RaiseUserUnmuted(object? sender, User actor, User obj) =>
		UserUnmuted?.Invoke(sender, new UserInteraction { Actor = actor, Object = obj });

	public void RaiseFilterAdded(object? sender, Filter filter)        => FilterAdded?.Invoke(sender, filter);
	public void RaiseFilterRemoved(object? sender, Filter filter)      => FilterRemoved?.Invoke(sender, filter);
	public void RaiseFilterUpdated(object? sender, Filter filter)      => FilterUpdated?.Invoke(sender, filter);
	public void RaiseListMembersUpdated(object? sender, UserList list) => ListMembersUpdated?.Invoke(sender, list);
}