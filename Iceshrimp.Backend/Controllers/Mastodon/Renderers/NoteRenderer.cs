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
	                                      List<Attachment>? attachments = null,
	                                      Dictionary<string, int>? likeCounts = null, int recurse = 2
	) {
		var uri = note.Uri ?? $"https://{config.Value.WebDomain}/notes/{note.Id}";
		var renote = note.Renote != null && recurse > 0
			? await RenderAsync(note.Renote, accounts, mentions, attachments, likeCounts, --recurse)
			: null;
		var text = note.Text; //TODO: append quote uri

		var likeCount = likeCounts?.GetValueOrDefault(note.Id, 0) ?? await db.NoteLikes.CountAsync(p => p.Note == note);

		if (mentions == null) {
			mentions = await db.Users.Where(p => note.Mentions.Contains(p.Id))
			                   .Select(u => new Mention(u, config.Value.WebDomain))
			                   .ToListAsync();
		}
		else {
			mentions = [..mentions.Where(p => note.Mentions.Contains(p.Id))];
		}

		if (attachments == null) {
			attachments = await db.DriveFiles.Where(p => note.FileIds.Contains(p.Id))
			                      .Select(f => new Attachment {
				                      Id          = f.Id,
				                      Url         = f.WebpublicUrl ?? f.Url,
				                      Blurhash    = f.Blurhash,
				                      PreviewUrl  = f.ThumbnailUrl,
				                      Description = f.Comment,
				                      Metadata    = null,
				                      RemoteUrl   = f.Uri,
				                      Type        = Attachment.GetType(f.Type)
			                      })
			                      .ToListAsync();
		}
		else {
			attachments = [..attachments.Where(p => note.FileIds.Contains(p.Id))];
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
			CreatedAt      = note.CreatedAt.ToStringMastodon(),
			EditedAt       = note.UpdatedAt?.ToStringMastodon(),
			RepliesCount   = note.RepliesCount,
			RenoteCount    = note.RenoteCount,
			FavoriteCount  = likeCount,
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
			IsPinned       = false,
			Attachments    = attachments
		};

		return res;
	}

	private async Task<List<Mention>> GetMentions(IEnumerable<Note> notes) {
		var ids = notes.SelectMany(n => n.Mentions).Distinct();
		return await db.Users.Where(p => ids.Contains(p.Id))
		               .Select(u => new Mention(u, config.Value.WebDomain))
		               .ToListAsync();
	}

	private async Task<List<Attachment>> GetAttachments(IEnumerable<Note> notes) {
		var ids = notes.SelectMany(n => n.FileIds).Distinct();
		return await db.DriveFiles.Where(p => ids.Contains(p.Id))
		               .Select(f => new Attachment {
			               Id          = f.Id,
			               Url         = f.Url,
			               Blurhash    = f.Blurhash,
			               PreviewUrl  = f.ThumbnailUrl,
			               Description = f.Comment,
			               Metadata    = null,
			               RemoteUrl   = f.Uri,
			               Type        = Attachment.GetType(f.Type)
		               })
		               .ToListAsync();
	}

	internal async Task<List<Account>> GetAccounts(IEnumerable<User> users) {
		return (await userRenderer.RenderManyAsync(users.DistinctBy(p => p.Id))).ToList();
	}

	private async Task<Dictionary<string, int>> GetLikeCounts(IEnumerable<Note> notes) {
		return await db.NoteLikes.Where(p => notes.Contains(p.Note)).Select(p => p.NoteId).GroupBy(p => p)
		               .ToDictionaryAsync(p => p.First(), p => p.Count());
	}

	public async Task<IEnumerable<Status>> RenderManyAsync(IEnumerable<Note> notes, List<Account>? accounts = null) {
		var noteList = notes.ToList();
		accounts ??= await GetAccounts(noteList.Select(p => p.User));
		var mentions    = await GetMentions(noteList);
		var attachments = await GetAttachments(noteList);
		var likeCounts  = await GetLikeCounts(noteList);
		return await noteList.Select(p => RenderAsync(p, accounts, mentions, attachments, likeCounts)).AwaitAllAsync();
	}
}