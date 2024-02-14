using Iceshrimp.Backend.Core.Database.Tables;

namespace Iceshrimp.Backend.Core.Services;

public class EventService {
	public event EventHandler<Note>   NotePublished;
	public event EventHandler<string> NoteDeleted;

	public void RaiseNotePublished(object? sender, Note note) => NotePublished.Invoke(sender, note);
	public void RaiseNoteDeleted(object? sender, Note note)   => NoteDeleted.Invoke(sender, note.Id);
}