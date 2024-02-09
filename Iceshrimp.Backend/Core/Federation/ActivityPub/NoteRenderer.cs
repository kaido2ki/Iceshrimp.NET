using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;
using Iceshrimp.Backend.Core.Helpers.LibMfm.Conversion;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Core.Federation.ActivityPub;

public class NoteRenderer(IOptions<Config.InstanceSection> config) {
	public async Task<ASNote> RenderAsync(Note note) {
		var id     = $"https://{config.Value.WebDomain}/notes/{note.Id}";
		var userId = $"https://{config.Value.WebDomain}/users/{note.User.Id}";
		var replyId = note.ReplyId != null
			? new ASIdObject($"https://{config.Value.WebDomain}/notes/{note.ReplyId}")
			: null;

		List<ASIdObject> to = note.Visibility switch {
			Note.NoteVisibility.Public    => [new ASLink($"{Constants.ActivityStreamsNs}#Public")],
			Note.NoteVisibility.Followers => [new ASLink($"{userId}/followers")],
			Note.NoteVisibility.Specified => [], // FIXME
			_                             => []
		};

		List<ASIdObject> cc = note.Visibility switch {
			Note.NoteVisibility.Home => [new ASLink($"{Constants.ActivityStreamsNs}#Public")],
			_                        => []
		};

		return new ASNote {
			Id           = id,
			Content      = note.Text != null ? await MfmConverter.ToHtmlAsync(note.Text) : null,
			AttributedTo = [new ASIdObject(userId)],
			Type         = $"{Constants.ActivityStreamsNs}#Note",
			MkContent    = note.Text,
			PublishedAt  = note.CreatedAt,
			Sensitive    = note.Cw != null,
			InReplyTo    = replyId,
			Source = note.Text != null
				? new ASNoteSource {
					Content   = note.Text,
					MediaType = "text/x.misskeymarkdown"
				}
				: null,
			Cc = cc,
			To = to
		};
	}
}