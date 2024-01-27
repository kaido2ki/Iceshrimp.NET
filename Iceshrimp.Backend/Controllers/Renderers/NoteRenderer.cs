using Iceshrimp.Backend.Controllers.Schemas;
using Iceshrimp.Backend.Core.Database.Tables;

namespace Iceshrimp.Backend.Controllers.Renderers;

public class NoteRenderer {
	public static NoteResponse RenderOne(Note note) {
		return new NoteResponse {
			Id   = note.Id,
			Text = note.Text,
			User = UserRenderer.RenderOne(note.User)
		};
	}

	public static IEnumerable<NoteResponse> RenderMany(IEnumerable<Note> notes) {
		return notes.Select(RenderOne);
	}
}