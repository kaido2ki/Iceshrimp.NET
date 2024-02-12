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
	public async Task<Status> RenderAsync(Note note, List<Account>? accounts = null, List<Mention>? mentions = null,
	                                      int recurse = 2
	) {
		var uri = note.Uri ?? $"https://{config.Value.WebDomain}/notes/{note.Id}";
		var renote = note.Renote != null && recurse > 0
			? await RenderAsync(note.Renote, accounts, mentions, --recurse)
			: null;
		var text = note.Text; //TODO: append quote uri

		if (mentions == null) {
			mentions = await db.Users.Where(p => note.Mentions.Contains(p.Id))
			                   .Select(u => new Mention(u, config.Value.WebDomain))
			                   .ToListAsync();
		}
		else {
			mentions = [..mentions.Where(p => note.Mentions.Contains(p.Id))];
		}

		var mentionedUsers = mentions.Select(p => new Note.MentionedUser {
			Host     = p.Host ?? config.Value.AccountDomain,
			Uri      = p.Uri,
			Username = p.Username,
			Url      = p.Url
		}).ToList();

		var content = text != null
			? await mfmConverter.ToHtmlAsync(text, mentionedUsers, note.UserHost)
			: null;

		var account = accounts?.FirstOrDefault(p => p.Id == note.UserId) ?? await userRenderer.RenderAsync(note.User);

		var res = new Status {
			Id             = note.Id,
			Uri            = uri,
			Url            = note.Url ?? uri,
			Account        = account,
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

	private async Task<List<Mention>> GetMentions(IEnumerable<Note> notes) {
		var ids = notes.SelectMany(n => n.Mentions).Distinct();
		return await db.Users.Where(p => ids.Contains(p.Id))
		               .Select(u => new Mention(u, config.Value.WebDomain))
		               .ToListAsync();
	}

	private async Task<List<Account>> GetAccounts(IEnumerable<User> users) {
		return (await userRenderer.RenderManyAsync(users.DistinctBy(p => p.Id))).ToList();
	}

	public async Task<IEnumerable<Status>> RenderManyAsync(IEnumerable<Note> notes) {
		var noteList = notes.ToList();
		var accounts = await GetAccounts(noteList.Select(p => p.User));
		var mentions = await GetMentions(noteList);
		return await noteList.Select(async p => await RenderAsync(p, accounts, mentions)).AwaitAllAsync();
	}
}