using Iceshrimp.Backend.Controllers.Mastodon.Schemas.Entities;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Helpers.LibMfm.Conversion;
using Iceshrimp.Backend.Core.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Controllers.Mastodon.Renderers;

public class NoteRenderer(
	IOptions<Config.InstanceSection> config,
	UserRenderer userRenderer,
	PollRenderer pollRenderer,
	MfmConverter mfmConverter,
	DatabaseContext db,
	EmojiService emojiSvc
)
{
	public async Task<StatusEntity> RenderAsync(Note note, User? user, NoteRendererDto? data = null, int recurse = 2)
	{
		var uri = note.Uri ?? note.GetPublicUri(config.Value);
		var renote = note is { Renote: not null, IsQuote: false } && recurse > 0
			? await RenderAsync(note.Renote, user, data, 0)
			: null;
		var quote = note is { Renote: not null, IsQuote: true } && recurse > 0
			? await RenderAsync(note.Renote, user, data, --recurse)
			: null;
		var text = note.Text;
		if (note is { Renote: not null, IsQuote: true } && text != null)
		{
			var quoteUri = note.Renote?.Url ?? note.Renote?.Uri ?? note.Renote?.GetPublicUriOrNull(config.Value);
			if (quoteUri != null)
				text += $"\n\nRE: {quoteUri}"; //TODO: render as inline quote
		}

		var liked = data?.LikedNotes?.Contains(note.Id) ??
		            await db.NoteLikes.AnyAsync(p => p.Note == note && p.User == user);
		var bookmarked = data?.BookmarkedNotes?.Contains(note.Id) ??
		                 await db.NoteBookmarks.AnyAsync(p => p.Note == note && p.User == user);
		var pinned = data?.PinnedNotes?.Contains(note.Id) ??
		             await db.UserNotePins.AnyAsync(p => p.Note == note && p.User == user);
		var renoted = data?.Renotes?.Contains(note.Id) ??
		              await db.Notes.AnyAsync(p => p.Renote == note && p.User == user && p.IsPureRenote);

		var noteEmoji = data?.Emoji?.Where(p => note.Emojis.Contains(p.Id)).ToList() ?? await GetEmoji([note]);

		var mentions = data?.Mentions == null
			? await GetMentions([note])
			: [..data.Mentions.Where(p => note.Mentions.Contains(p.Id))];

		var attachments = data?.Attachments == null
			? await GetAttachments([note])
			: [..data.Attachments.Where(p => note.FileIds.Contains(p.Id))];

		var reactions = data?.Reactions == null
			? await GetReactions([note], user)
			: [..data.Reactions.Where(p => p.NoteId == note.Id)];

		var mentionedUsers = mentions.Select(p => new Note.MentionedUser
		                             {
			                             Host     = p.Host ?? config.Value.AccountDomain,
			                             Uri      = p.Uri,
			                             Username = p.Username,
			                             Url      = p.Url
		                             })
		                             .ToList();

		var content = text != null && data?.Source != true
			? await mfmConverter.ToHtmlAsync(text, mentionedUsers, note.UserHost)
			: null;

		var account = data?.Accounts?.FirstOrDefault(p => p.Id == note.UserId) ??
		              await userRenderer.RenderAsync(note.User);

		var poll = note.HasPoll
			? (data?.Polls ?? await GetPolls([note], user)).FirstOrDefault(p => p.Id == note.Id)
			: null;

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
			CreatedAt      = note.CreatedAt.ToStringIso8601Like(),
			EditedAt       = note.UpdatedAt?.ToStringIso8601Like(),
			RepliesCount   = note.RepliesCount,
			RenoteCount    = note.RenoteCount,
			FavoriteCount  = note.LikeCount,
			IsFavorited    = liked,
			IsRenoted      = renoted,
			IsBookmarked   = bookmarked,
			IsMuted        = null, //FIXME
			IsSensitive    = note.Cw != null,
			ContentWarning = note.Cw ?? "",
			Visibility     = StatusEntity.EncodeVisibility(note.Visibility),
			Content        = content,
			Text           = data?.Source == true ? text : null,
			Mentions       = mentions,
			IsPinned       = pinned,
			Attachments    = attachments,
			Emojis         = noteEmoji,
			Poll           = poll,
			Reactions      = reactions
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
			               Url         = f.PublicUrl,
			               Blurhash    = f.Blurhash,
			               PreviewUrl  = f.PublicThumbnailUrl,
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

	private async Task<List<string>> GetLikedNotes(IEnumerable<Note> notes, User? user)
	{
		if (user == null) return [];
		return await db.NoteLikes.Where(p => p.User == user && notes.Contains(p.Note))
		               .Select(p => p.NoteId)
		               .ToListAsync();
	}

	private async Task<List<ReactionEntity>> GetReactions(List<Note> notes, User? user)
	{
		if (user == null) return [];
		var counts = notes.ToDictionary(p => p.Id, p => p.Reactions);
		var res = await db.NoteReactions
		                  .Where(p => notes.Contains(p.Note))
		                  .GroupBy(p => p.Reaction)
		                  .Select(p => new ReactionEntity
		                  {
			                  NoteId = p.First().NoteId,
			                  Count  = (int)counts[p.First().NoteId].GetValueOrDefault(p.First().Reaction, 1),
			                  Me = db.NoteReactions.Any(i => i.NoteId == p.First().NoteId &&
			                                                 i.Reaction == p.First().Reaction &&
			                                                 i.User == user),
			                  Name      = p.First().Reaction,
			                  Url       = null,
			                  StaticUrl = null
		                  })
		                  .ToListAsync();

		foreach (var item in res.Where(item => item.Name.StartsWith(':')))
		{
			var hit = await emojiSvc.ResolveEmoji(item.Name);
			if (hit == null) continue;
			item.Url       = hit.PublicUrl;
			item.StaticUrl = hit.PublicUrl;
		}

		return res;
	}

	private async Task<List<string>> GetBookmarkedNotes(IEnumerable<Note> notes, User? user)
	{
		if (user == null) return [];
		return await db.NoteBookmarks.Where(p => p.User == user && notes.Contains(p.Note))
		               .Select(p => p.NoteId)
		               .ToListAsync();
	}

	private async Task<List<string>> GetPinnedNotes(IEnumerable<Note> notes, User? user)
	{
		if (user == null) return [];
		return await db.UserNotePins.Where(p => p.User == user && notes.Contains(p.Note))
		               .Select(p => p.NoteId)
		               .ToListAsync();
	}

	private async Task<List<string>> GetRenotes(IEnumerable<Note> notes, User? user)
	{
		if (user == null) return [];
		return await db.Notes.Where(p => p.User == user && p.IsPureRenote && notes.Contains(p.Renote))
		               .Select(p => p.RenoteId)
		               .Where(p => p != null)
		               .Distinct()
		               .Cast<string>()
		               .ToListAsync();
	}

	private async Task<List<PollEntity>> GetPolls(IEnumerable<Note> notes, User? user)
	{
		var polls = await db.Polls.Where(p => notes.Contains(p.Note))
		                    .ToListAsync();

		return await pollRenderer.RenderManyAsync(polls, user).ToListAsync();
	}

	private async Task<List<EmojiEntity>> GetEmoji(IEnumerable<Note> notes)
	{
		var ids = notes.SelectMany(p => p.Emojis).ToList();
		if (ids.Count == 0) return [];

		return await db.Emojis
		               .Where(p => ids.Contains(p.Id))
		               .Select(p => new EmojiEntity
		               {
			               Id              = p.Id,
			               Shortcode       = p.Name,
			               Url             = p.PublicUrl,
			               StaticUrl       = p.PublicUrl, //TODO
			               VisibleInPicker = true
		               })
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

		var data = new NoteRendererDto
		{
			Accounts        = accounts ?? await GetAccounts(noteList.Select(p => p.User)),
			Mentions        = await GetMentions(noteList),
			Attachments     = await GetAttachments(noteList),
			Polls           = await GetPolls(noteList, user),
			LikedNotes      = await GetLikedNotes(noteList, user),
			BookmarkedNotes = await GetBookmarkedNotes(noteList, user),
			PinnedNotes     = await GetPinnedNotes(noteList, user),
			Renotes         = await GetRenotes(noteList, user),
			Emoji           = await GetEmoji(noteList),
			Reactions       = await GetReactions(noteList, user)
		};

		return await noteList.Select(p => RenderAsync(p, user, data)).AwaitAllAsync();
	}

	public class NoteRendererDto
	{
		public List<AccountEntity>?    Accounts;
		public List<MentionEntity>?    Mentions;
		public List<AttachmentEntity>? Attachments;
		public List<PollEntity>?       Polls;
		public List<string>?           LikedNotes;
		public List<string>?           BookmarkedNotes;
		public List<string>?           PinnedNotes;
		public List<string>?           Renotes;
		public List<EmojiEntity>?      Emoji;
		public List<ReactionEntity>?   Reactions;

		public bool Source;
	}
}