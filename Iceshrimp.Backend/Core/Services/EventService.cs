using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Events;

namespace Iceshrimp.Backend.Core.Services;

public class EventService {
	public event EventHandler<Note>?            NotePublished;
	public event EventHandler<string>?          NoteDeleted;
	public event EventHandler<NoteInteraction>? NoteLiked;
	public event EventHandler<NoteInteraction>? NoteUnliked;
	public event EventHandler<Notification>?    Notification;

	public void RaiseNotePublished(object? sender, Note note) => NotePublished?.Invoke(sender, note);
	public void RaiseNoteDeleted(object? sender, Note note)   => NoteDeleted?.Invoke(sender, note.Id);

	public void RaiseNotification(object? sender, Notification notification) =>
		Notification?.Invoke(sender, notification);

	public void RaiseNotifications(object? sender, IEnumerable<Notification> notifications) {
		foreach (var notification in notifications) Notification?.Invoke(sender, notification);
	}

	public void RaiseNoteLiked(object? sender, Note note, User user) =>
		NoteLiked?.Invoke(sender, new NoteInteraction { Note = note, User = user });

	public void RaiseNoteUnliked(object? sender, Note note, User user) =>
		NoteUnliked?.Invoke(sender, new NoteInteraction { Note = note, User = user });
}