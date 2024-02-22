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
)
{
	public async Task<StatusEntity> RenderAsync(
		Note note, User? user, List<AccountEntity>? accounts = null, List<MentionEntity>? mentions = null,
		List<AttachmentEntity>? attachments = null, Dictionary<string, int>? likeCounts = null,
		List<string>? likedNotes = null, int recurse = 2
	)
	{
		var uri = note.Uri ?? note.GetPublicUri(config.Value);
		var renote = note is { Renote: not null, IsQuote: false } && recurse > 0
			? await RenderAsync(note.Renote, user, accounts, mentions, attachments, likeCounts, likedNotes, 0)
			: null;
		var quote = note is { Renote: not null, IsQuote: true } && recurse > 0
			? await RenderAsync(note.Renote, user, accounts, mentions, attachments, likeCounts, likedNotes, --recurse)
			: null;
		var text = note.Text;
		if (note is { Renote: not null, IsQuote: true } && text != null)
		{
			var quoteUri = note.Renote?.Url ?? note.Renote?.Uri ?? note.Renote?.GetPublicUriOrNull(config.Value);
			if (quoteUri != null)
				text += $"\n\nRE: {quoteUri}"; //TODO: render as inline quote
		}

		var likeCount = likeCounts?.GetValueOrDefault(note.Id, 0) ?? await db.NoteLikes.CountAsync(p => p.Note == note);
		var liked = likedNotes?.Contains(note.Id) ?? await db.NoteLikes.AnyAsync(p => p.Note == note && p.User == user);

		if (mentions == null)
		{
			mentions = await db.Users.IncludeCommonProperties()
			                   .Where(p => note.Mentions.Contains(p.Id))
			                   .Select(u => new MentionEntity(u, config.Value.WebDomain))
			                   .ToListAsync();
		}
		else
		{
			mentions = [..mentions.Where(p => note.Mentions.Contains(p.Id))];
		}

		if (attachments == null)
		{
			attachments = await db.DriveFiles.Where(p => note.FileIds.Contains(p.Id))
			                      .Select(f => new AttachmentEntity
			                      {
				                      Id          = f.Id,
				                      Url         = f.WebpublicUrl ?? f.Url,
				                      Blurhash    = f.Blurhash,
				                      PreviewUrl  = f.ThumbnailUrl,
				                      Description = f.Comment,
				                      Metadata    = null,
				                      RemoteUrl   = f.Uri,
				                      Type        = AttachmentEntity.GetType(f.Type)
			                      })
			                      .ToListAsync();
		}
		else
		{
			attachments = [..attachments.Where(p => note.FileIds.Contains(p.Id))];
		}

		var mentionedUsers = mentions.Select(p => new Note.MentionedUser
		                             {
			                             Host     = p.Host ?? config.Value.AccountDomain,
			                             Uri      = p.Uri,
			                             Username = p.Username,
			                             Url      = p.Url
		                             })
		                             .ToList();

		var content = text != null
			? await mfmConverter.ToHtmlAsync(text, mentionedUsers, note.UserHost)
			: null;

		var account = accounts?.FirstOrDefault(p => p.Id == note.UserId) ?? await userRenderer.RenderAsync(note.User);

		var res = new StatusEntity
		{
			Id             = note.Id,
			Uri            = uri,
			Url            = note.Url ?? uri,
			Account        = account,
			ReplyId        = note.ReplyId,
			ReplyUserId    = note.ReplyUserId,
			Renote         = renote,
			Quote          = quote,
			ContentType    = "text/x.misskeymarkdown",
			CreatedAt      = note.CreatedAt.ToStringMastodon(),
			EditedAt       = note.UpdatedAt?.ToStringMastodon(),
			RepliesCount   = note.RepliesCount,
			RenoteCount    = note.RenoteCount,
			FavoriteCount  = likeCount,
			IsFavorited    = liked,
			IsRenoted      = false, //FIXME
			IsBookmarked   = false, //FIXME
			IsMuted        = null,  //FIXME
			IsSensitive    = note.Cw != null,
			ContentWarning = note.Cw ?? "",
			Visibility     = StatusEntity.EncodeVisibility(note.Visibility),
			Content        = content,
			Text           = text,
			Mentions       = mentions,
			IsPinned       = false,
			Attachments    = attachments
		};

		return res;
	}

	private async Task<List<MentionEntity>> GetMentions(IEnumerable<Note> notes)
	{
		var ids = notes.SelectMany(n => n.Mentions).Distinct();
		return await db.Users.IncludeCommonProperties()
		               .Where(p => ids.Contains(p.Id))
		               .Select(u => new MentionEntity(u, config.Value.WebDomain))
		               .ToListAsync();
	}

	private async Task<List<AttachmentEntity>> GetAttachments(IEnumerable<Note> notes)
	{
		var ids = notes.SelectMany(n => n.FileIds).Distinct();
		return await db.DriveFiles.Where(p => ids.Contains(p.Id))
		               .Select(f => new AttachmentEntity
		               {
			               Id          = f.Id,
			               Url         = f.Url,
			               Blurhash    = f.Blurhash,
			               PreviewUrl  = f.ThumbnailUrl,
			               Description = f.Comment,
			               Metadata    = null,
			               RemoteUrl   = f.Uri,
			               Type        = AttachmentEntity.GetType(f.Type)
		               })
		               .ToListAsync();
	}

	internal async Task<List<AccountEntity>> GetAccounts(IEnumerable<User> users)
	{
		return (await userRenderer.RenderManyAsync(users.DistinctBy(p => p.Id))).ToList();
	}

	private async Task<Dictionary<string, int>> GetLikeCounts(IEnumerable<Note> notes)
	{
		return await db.NoteLikes.Where(p => notes.Contains(p.Note))
		               .Select(p => p.NoteId)
		               .GroupBy(p => p)
		               .ToDictionaryAsync(p => p.First(), p => p.Count());
	}

	private async Task<List<string>> GetLikedNotes(IEnumerable<Note> notes, User? user)
	{
		if (user == null) return [];
		return await db.NoteLikes.Where(p => p.User == user && notes.Contains(p.Note))
		               .Select(p => p.NoteId)
		               .ToListAsync();
	}

	public async Task<IEnumerable<StatusEntity>> RenderManyAsync(
		IEnumerable<Note> notes, User? user, List<AccountEntity>? accounts = null
	)
	{
		var noteList = notes.SelectMany<Note, Note?>(p => [p, p.Renote])
		                    .Where(p => p != null)
		                    .Cast<Note>()
		                    .DistinctBy(p => p.Id)
		                    .ToList();

		accounts ??= await GetAccounts(noteList.Select(p => p.User));
		var mentions    = await GetMentions(noteList);
		var attachments = await GetAttachments(noteList);
		var likeCounts  = await GetLikeCounts(noteList);
		var likedNotes  = await GetLikedNotes(noteList, user);
		return await noteList.Select(p => RenderAsync(p, user, accounts, mentions, attachments, likeCounts, likedNotes))
		                     .AwaitAllAsync();
	}
}