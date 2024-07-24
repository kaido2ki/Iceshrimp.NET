using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Events;

namespace Iceshrimp.Backend.Core.Services;

public interface IEventService
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

	public Task RaiseNotePublished(object? sender, Note note);
	public Task RaiseNoteUpdated(object? sender, Note note);
	public Task RaiseNoteDeleted(object? sender, Note note);
	public Task RaiseNotification(object? sender, Notification notification);
	public Task RaiseNotifications(object? sender, IEnumerable<Notification> notifications);
	public Task RaiseNoteLiked(object? sender, Note note, User user);
	public Task RaiseNoteUnliked(object? sender, Note note, User user);
	public Task RaiseNoteReacted(object? sender, NoteReaction reaction);
	public Task RaiseNoteUnreacted(object? sender, NoteReaction reaction);
	public Task RaiseUserFollowed(object? sender, User actor, User obj);
	public Task RaiseUserUnfollowed(object? sender, User actor, User obj);
	public Task RaiseUserBlocked(object? sender, User actor, User obj);
	public Task RaiseUserUnblocked(object? sender, User actor, User obj);
	public Task RaiseUserMuted(object? sender, User actor, User obj);
	public Task RaiseUserUnmuted(object? sender, User actor, User obj);
	public Task RaiseFilterAdded(object? sender, Filter filter);
	public Task RaiseFilterRemoved(object? sender, Filter filter);
	public Task RaiseFilterUpdated(object? sender, Filter filter);
	public Task RaiseListMembersUpdated(object? sender, UserList list);
}