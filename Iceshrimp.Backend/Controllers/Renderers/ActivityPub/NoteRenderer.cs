using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Controllers.Renderers.ActivityPub;

public class NoteRenderer(IOptions<Config.InstanceSection> config) {
	public ASNote Render(Note note) {
		var id     = $"https://{config.Value.WebDomain}/notes/{note.Id}";
		var userId = $"https://{config.Value.WebDomain}/users/{note.User.Id}";

		return new ASNote {
			Id           = id,
			Content      = note.Text, //FIXME: render to html
			AttributedTo = [new LDIdObject(userId)],
			Type         = "https://www.w3.org/ns/activitystreams#Note",
			MkContent    = note.Text,
			PublishedAt  = note.CreatedAt,
			Sensitive    = note.Cw != null,
			Source = new ASNoteSource {
				Content   = note.Text,
				MediaType = "text/x.misskeymarkdown"
			},
			//TODO: implement this properly
			Cc = [new ASLink("https://www.w3.org/ns/activitystreams#Public")],
			To = []
		};
	}
}