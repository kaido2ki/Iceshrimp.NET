using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Events;
using Iceshrimp.Utils.DependencyInjection;

namespace Iceshrimp.Backend.Core.Services;

public class EventService : ISingletonService
{
	public delegate void EventHandler<in TArgs>(TArgs e);

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
	public event EventHandler<UserInteraction>? UserRenotesMuted;
	public event EventHandler<UserInteraction>? UserRenotesUnmuted;
	public event EventHandler<Notification>?    Notification;
	public event EventHandler<Filter>?          FilterAdded;
	public event EventHandler<Filter>?          FilterRemoved;
	public event EventHandler<Filter>?          FilterUpdated;
	public event EventHandler<UserList>?        ListMembersUpdated;
	public event EventHandler<BubbleInstance>?  BubbleInstanceAdded;
	public event EventHandler<BubbleInstance>?  BubbleInstanceRemoved;

	public void RaiseNotePublished(Note note)                => NotePublished?.Invoke(note);
	public void RaiseNoteUpdated(Note note)                  => NoteUpdated?.Invoke(note);
	public void RaiseNoteDeleted(Note note)                  => NoteDeleted?.Invoke(note);
	public void RaiseNotification(Notification notification) => Notification?.Invoke(notification);

	public void RaiseNotifications(IEnumerable<Notification> notifications)
	{
		foreach (var notification in notifications) Notification?.Invoke(notification);
	}

	public void RaiseNoteLiked(Note note, User user)
		=> NoteLiked?.Invoke(new NoteInteraction { Note = note, User = user });

	public void RaiseNoteUnliked(Note note, User user)
		=> NoteUnliked?.Invoke(new NoteInteraction { Note = note, User = user });

	public void RaiseNoteReacted(NoteReaction reaction)   => NoteReacted?.Invoke(reaction);
	public void RaiseNoteUnreacted(NoteReaction reaction) => NoteUnreacted?.Invoke(reaction);

	public void RaiseUserFollowed(User actor, User obj)
		=> UserFollowed?.Invoke(new UserInteraction { Actor = actor, Object = obj });

	public void RaiseUserUnfollowed(User actor, User obj)
		=> UserUnfollowed?.Invoke(new UserInteraction { Actor = actor, Object = obj });

	public void RaiseUserBlocked(User actor, User obj)
		=> UserBlocked?.Invoke(new UserInteraction { Actor = actor, Object = obj });

	public void RaiseUserUnblocked(User actor, User obj)
		=> UserUnblocked?.Invoke(new UserInteraction { Actor = actor, Object = obj });

	public void RaiseUserMuted(User actor, User obj)
		=> UserMuted?.Invoke(new UserInteraction { Actor = actor, Object = obj });

	public void RaiseUserUnmuted(User actor, User obj)
		=> UserUnmuted?.Invoke(new UserInteraction { Actor = actor, Object = obj });

	public void RaiseUserRenotesMuted(User actor, User obj)
		=> UserRenotesMuted?.Invoke(new UserInteraction { Actor = actor, Object = obj });

	public void RaiseUserRenotesUnmuted(User actor, User obj)
		=> UserRenotesUnmuted?.Invoke(new UserInteraction { Actor = actor, Object = obj });

	public void RaiseFilterAdded(Filter filter)                     => FilterAdded?.Invoke(filter);
	public void RaiseFilterRemoved(Filter filter)                   => FilterRemoved?.Invoke(filter);
	public void RaiseFilterUpdated(Filter filter)                   => FilterUpdated?.Invoke(filter);
	public void RaiseListMembersUpdated(UserList list)              => ListMembersUpdated?.Invoke(list);
	public void RaiseBubbleInstanceAdded(BubbleInstance instance)   => BubbleInstanceAdded?.Invoke(instance);
	public void RaiseBubbleInstanceRemoved(BubbleInstance instance) => BubbleInstanceRemoved?.Invoke(instance);
}
