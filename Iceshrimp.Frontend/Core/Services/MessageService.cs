using System.ComponentModel;
using Iceshrimp.Shared.Schemas.Web;

namespace Iceshrimp.Frontend.Core.Services;

internal class MessageService
{
	public event EventHandler<NoteResponse>?               AnyNoteChanged;

	public Dictionary<string, EventHandler<NoteResponse>> NoteChangedHandlers = new();

	public void Register(string id, EventHandler<NoteResponse> func)
	{
		if (NoteChangedHandlers.ContainsKey(id))
		{
			NoteChangedHandlers[id] += func;
		}
		else
		{
			NoteChangedHandlers.Add(id, func);
		}
	}

	public void Unregister(string id, EventHandler<NoteResponse> func)
	{
		NoteChangedHandlers[id] -= func;
	}

	public Task UpdateNote(NoteResponse note)
	{
		AnyNoteChanged?.Invoke(this, note);
		NoteChangedHandlers.TryGetValue(note.Id, out var xHandler);
		xHandler?.Invoke(this, note);
		return Task.CompletedTask;
	}
}