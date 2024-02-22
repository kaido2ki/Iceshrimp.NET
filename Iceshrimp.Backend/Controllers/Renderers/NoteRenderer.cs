using Iceshrimp.Backend.Controllers.Schemas;
using Iceshrimp.Backend.Core.Database.Tables;

namespace Iceshrimp.Backend.Controllers.Renderers;

public class NoteRenderer(UserRenderer userRenderer)
{
	public NoteResponse RenderOne(Note note)
	{
		return new NoteResponse { Id = note.Id, Text = note.Text, User = userRenderer.RenderOne(note.User) };
	}

	public IEnumerable<NoteResponse> RenderMany(IEnumerable<Note> notes)
	{
		return notes.Select(RenderOne);
	}
}