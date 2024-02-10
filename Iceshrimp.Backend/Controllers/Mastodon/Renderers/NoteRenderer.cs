using Iceshrimp.Backend.Controllers.Mastodon.Schemas.Entities;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Helpers.LibMfm.Conversion;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Controllers.Mastodon.Renderers;

public class NoteRenderer(
	IOptions<Config.InstanceSection> config,
	UserRenderer userRenderer,
	MfmConverter mfmConverter,
	DatabaseContext db
) {
	public async Task<Status> RenderAsync(Note note, List<Mention>? mentions = null, int recurse = 2) {
		var uri     = note.Uri ?? $"https://{config.Value.WebDomain}/notes/{note.Id}";
		var renote  = note.Renote != null && recurse > 0 ? await RenderAsync(note.Renote, mentions, --recurse) : null;
		var text    = note.Text; //TODO: append quote uri
		var content = text != null ? await mfmConverter.ToHtmlAsync(text, note.MentionedRemoteUsers) : null;

		if (mentions == null) {
			mentions = await db.Users.Where(p => note.Mentions.Contains(p.Id))
			                   .Select(u => new Mention {
				                   Id       = u.Id,
				                   Username = u.Username,
				                   Acct     = u.Acct,
				                   Url = (u.UserProfile != null
						                   ? u.UserProfile.Url ?? u.Uri
						                   : u.Uri) ?? $"https://{config.Value.WebDomain}/@{u.Username}"
			                   })
			                   .ToListAsync();
		}
		else {
			mentions = [..mentions.Where(p => note.Mentions.Contains(p.Id))];
		}

		var res = new Status {
			Id             = note.Id,
			Uri            = uri,
			Url            = note.Url ?? uri,
			Account        = await userRenderer.RenderAsync(note.User), //TODO: batch this
			ReplyId        = note.ReplyId,
			ReplyUserId    = note.ReplyUserId,
			Renote         = renote, //TODO: check if it's a pure renote
			Quote          = renote, //TODO: see above
			ContentType    = "text/x.misskeymarkdown",
			CreatedAt      = note.CreatedAt.ToString("O")[..^5],
			EditedAt       = note.UpdatedAt?.ToString("O")[..^5],
			RepliesCount   = note.RepliesCount,
			RenoteCount    = note.RenoteCount,
			FavoriteCount  = 0,     //FIXME
			IsRenoted      = false, //FIXME
			IsFavorited    = false, //FIXME
			IsBookmarked   = false, //FIXME
			IsMuted        = null,  //FIXME
			IsSensitive    = note.Cw != null,
			ContentWarning = note.Cw ?? "",
			Visibility     = Status.EncodeVisibility(note.Visibility),
			Content        = content,
			Text           = text,
			Mentions       = mentions,
			IsPinned       = false //FIXME
		};

		return res;
	}

	private async Task<List<Mention>> GetMentions(IReadOnlyCollection<Note> notes) {
		var ids = notes.SelectMany(n => n.Mentions).Distinct();
		return await db.Users.Where(p => ids.Contains(p.Id))
		               .Select(u => new Mention {
			               Id       = u.Id,
			               Username = u.Username,
			               Acct     = u.Acct,
			               Url = u.UserProfile != null
				               ? u.UserProfile.Url ?? u.Uri ?? $"https://{config.Value.WebDomain}/@{u.Username}"
				               : u.Uri ?? $"https://{config.Value.WebDomain}/@{u.Username}"
		               })
		               .ToListAsync();
	}

	public async Task<IEnumerable<Status>> RenderManyAsync(IEnumerable<Note> notes) {
		var noteList = notes.ToList();
		var mentions = await GetMentions(noteList);
		return await noteList.Select(async p => await RenderAsync(p, mentions)).AwaitAllAsync();
	}
}