using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;
using Iceshrimp.Backend.Core.Helpers.LibMfm.Conversion;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Core.Federation.ActivityPub;

public class NoteRenderer(IOptions<Config.InstanceSection> config, MfmConverter mfmConverter, DatabaseContext db) {
	public async Task<ASNote> RenderAsync(Note note, List<Note.MentionedUser>? mentions = null) {
		var id     = $"https://{config.Value.WebDomain}/notes/{note.Id}";
		var userId = $"https://{config.Value.WebDomain}/users/{note.User.Id}";
		var replyId = note.Reply != null
			? new ASObjectBase(note.Reply.Uri ?? $"https://{config.Value.WebDomain}/notes/{note.ReplyId}")
			: null;

		mentions ??= await db.Users
		                     .Where(p => note.Mentions.Contains(p.Id))
		                     .IncludeCommonProperties()
		                     .Select(p => new Note.MentionedUser {
			                     Host     = p.Host ?? config.Value.AccountDomain,
			                     Username = p.Username,
			                     Url      = p.UserProfile != null ? p.UserProfile.Url : null,
			                     Uri      = p.Uri ?? $"https://{config.Value.WebDomain}/users/{p.Id}"
		                     })
		                     .ToListAsync();

		var to = note.Visibility switch {
			Note.NoteVisibility.Public    => [new ASLink($"{Constants.ActivityStreamsNs}#Public")],
			Note.NoteVisibility.Followers => [new ASLink($"{userId}/followers")],
			Note.NoteVisibility.Specified => mentions.Select(p => new ASObjectBase(p.Uri)).ToList(),
			_                             => []
		};

		List<ASObjectBase> cc = note.Visibility switch {
			Note.NoteVisibility.Home => [new ASLink($"{Constants.ActivityStreamsNs}#Public")],
			_                        => []
		};

		var tags = mentions
		           .Select(mention => new ASMention {
			           Type = $"{Constants.ActivityStreamsNs}#Mention",
			           Name = $"@{mention.Username}@{mention.Host}",
			           Href = new ASObjectBase(mention.Uri)
		           })
		           .Cast<ASTag>()
		           .ToList();

		var attachments = note.FileIds.Count > 0
			? await db.DriveFiles
			          .Where(p => note.FileIds.Contains(p.Id) && p.UserHost == null)
			          .Select(p => new ASDocument {
				          Type      = $"{Constants.ActivityStreamsNs}#Document",
				          Sensitive = p.IsSensitive,
				          Url       = new ASObjectBase(p.Url),
				          MediaType = p.Type,
				          Description = p.Comment
			          })
			          .Cast<ASAttachment>()
			          .ToListAsync()
			: null;

		return new ASNote {
			Id           = id,
			AttributedTo = [new ASObjectBase(userId)],
			Type         = $"{Constants.ActivityStreamsNs}#Note",
			MkContent    = note.Text,
			PublishedAt  = note.CreatedAt,
			Sensitive    = note.Cw != null,
			InReplyTo    = replyId,
			Cc           = cc,
			To           = to,
			Tags         = tags,
			Attachments  = attachments,
			Content = note.Text != null
				? await mfmConverter.ToHtmlAsync(note.Text, mentions, note.UserHost)
				: null,
			Source = note.Text != null
				? new ASNoteSource {
					Content   = note.Text,
					MediaType = "text/x.misskeymarkdown"
				}
				: null
		};
	}
}