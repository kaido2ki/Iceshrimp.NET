using Iceshrimp.Backend.Controllers.Schemas;
using Iceshrimp.Backend.Core.Database.Tables;

namespace Iceshrimp.Backend.Controllers.Renderers;

public class TimelineRenderer
{
	public static TimelineResponse Render(IEnumerable<Note> notes, int limit)
	{
		return new TimelineResponse { Notes = NoteRenderer.RenderMany(notes), Limit = limit };
	}
}