using Iceshrimp.Frontend.Components.Note;
using Microsoft.AspNetCore.Components;

namespace Iceshrimp.Frontend.Core.Miscellaneous;

public abstract class ChildNoteController : ComponentBase
{
	public List<NoteBody> NoteChildren { get; set; } = [];

	public void Register(NoteBody note)
	{
		NoteChildren.Add(note);
	}

	public void Unregister(NoteBody note)
	{
		NoteChildren.Remove(note);
	}
}
