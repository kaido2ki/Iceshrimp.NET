using System.Globalization;
using Iceshrimp.Backend.Controllers.Mastodon.Schemas.Entities;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Helpers;
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
	public async Task<StatusEntity> RenderAsync(
		Note note, User? user, Filter.FilterContext? filterContext = null, NoteRendererDto? data = null, int recurse = 2
	)
	{
		var uri = note.Uri ?? note.GetPublicUri(config.Value);
		var renote = note is { Renote: not null, IsQuote: false } && recurse > 1
			? await RenderAsync(note.Renote, user, null, data, --recurse)
			: null;
		var quote = note is { Renote: not null, IsQuote: true } && recurse > 0
			? await RenderAsync(note.Renote, user, null, data, 0)
			: null;
		var     text     = note.Text;
		string? quoteUri = null;

		if (note is { Renote: not null, IsQuote: true } && text != null)
		{
			var qUri = note.Renote?.Url ?? note.Renote?.Uri ?? note.Renote?.GetPublicUriOrNull(config.Value);
			var alt  = note.Renote?.Uri;

			if (qUri != null && !text.Contains(qUri) && (alt == null || qUri == alt || !text.Contains(alt)))
				quoteUri = qUri;
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

		var replyInaccessible =
			note.Reply == null && ((note.ReplyId != null && recurse == 2) || note.ReplyUri != null);
		var quoteInaccessible =
			note.Renote == null && ((note.RenoteId != null && recurse > 0) || note.RenoteUri != null);

		var lang = note.Lang != null ? CultureInfo.GetCultureInfo(note.Lang).TwoLetterISOLanguageName : null;
		var content = data?.Source != true
			? text != null
				? await mfmConverter.ToHtmlAsync(text, mentionedUsers, note.UserHost, quoteUri, quoteInaccessible,
				                                 replyInaccessible)
				: ""
			: null;

		var account = data?.Accounts?.FirstOrDefault(p => p.Id == note.UserId) ??
		              await userRenderer.RenderAsync(note.User);

		var poll = note.HasPoll
			? (data?.Polls ?? await GetPolls([note], user)).FirstOrDefault(p => p.Id == note.Id)
			: null;

		var filters = data?.Filters ?? await GetFilters(user, filterContext);

		List<FilterResultEntity> filterResult;
		if (filters.Count > 0 && filterContext == null)
		{
			var filtered = FilterHelper.CheckFilters([note, note.Reply, note.Renote, note.Renote?.Renote], filters);
			filterResult = GetFilterResult(filtered);
		}
		else
		{
			var filtered = FilterHelper.IsFiltered([note, note.Reply, note.Renote, note.Renote?.Renote], filters);
			filterResult = GetFilterResult(filtered.HasValue ? [filtered.Value] : []);
		}

		if ((user?.UserSettings?.FilterInaccessible ?? false) && (replyInaccessible || quoteInaccessible))
			filterResult.Insert(0, InaccessibleFilter);

		var res = new StatusEntity
		{
			Id               = note.Id,
			Uri              = uri,
			Url              = note.Url ?? uri,
			Account          = account,
			ReplyId          = note.ReplyId,
			ReplyUserId      = note.MastoReplyUserId ?? note.ReplyUserId,
			MastoReplyUserId = note.MastoReplyUserId,
			Renote           = renote,
			Quote            = quote,
			QuoteId          = note.IsQuote ? note.RenoteId : null,
			ContentType      = "text/x.misskeymarkdown",
			CreatedAt        = note.CreatedAt.ToStringIso8601Like(),
			EditedAt         = note.UpdatedAt?.ToStringIso8601Like(),
			RepliesCount     = note.RepliesCount,
			RenoteCount      = note.RenoteCount,
			FavoriteCount    = note.LikeCount,
			IsFavorited      = liked,
			IsRenoted        = renoted,
			IsBookmarked     = bookmarked,
			IsMuted          = false, //FIXME
			IsSensitive      = note.Cw != null || attachments.Any(p => p.Sensitive),
			ContentWarning   = note.Cw ?? "",
			Visibility       = StatusEntity.EncodeVisibility(note.Visibility),
			Content          = content,
			Language         = lang,
			Text             = text,
			Mentions         = mentions,
			IsPinned         = pinned,
			Attachments      = attachments,
			Emojis           = noteEmoji,
			Poll             = poll,
			Reactions        = reactions,
			Filtered         = filterResult
		};

		return res;
	}

	private static readonly FilterResultEntity InaccessibleFilter = new()
	{
		Filter = new FilterEntity
		{
			Title        = "HideInaccessible",
			FilterAction = "hide",
			Id           = "0",
			Context      = ["home", "thread", "notifications", "account", "public"],
			Keywords     = [new FilterKeyword("RE: \ud83d\udd12", 0, 0)],
			ExpiresAt    = null
		},
		KeywordMatches = ["RE: \ud83d\udd12"] // lock emoji
	};

	public async Task<List<StatusEdit>> RenderHistoryAsync(Note note)
	{
		var edits = await db.NoteEdits.Where(p => p.Note == note).OrderBy(p => p.Id).ToListAsync();
		edits.Add(RenderEdit(note));

		var attachments = await GetAttachments(note.FileIds.Concat(edits.SelectMany(p => p.FileIds)));
		var mentions    = await GetMentions([note]);
		var mentionedUsers = mentions.Select(p => new Note.MentionedUser
		                             {
			                             Host     = p.Host ?? config.Value.AccountDomain,
			                             Uri      = p.Uri,
			                             Username = p.Username,
			                             Url      = p.Url
		                             })
		                             .ToList();

		var account  = await userRenderer.RenderAsync(note.User);
		var lastDate = note.CreatedAt;

		List<StatusEdit> history = [];
		foreach (var edit in edits)
		{
			var files = attachments.Where(p => edit.FileIds.Contains(p.Id)).ToList();
			var entry = new StatusEdit
			{
				Account        = account,
				Content        = await mfmConverter.ToHtmlAsync(edit.Text ?? "", mentionedUsers, note.UserHost),
				CreatedAt      = lastDate.ToStringIso8601Like(),
				Emojis         = [],
				IsSensitive    = files.Any(p => p.Sensitive),
				Attachments    = files,
				Poll           = null,
				ContentWarning = edit.Cw ?? ""
			};
			history.Add(entry);
			lastDate = edit.UpdatedAt;
		}

		return history;
	}

	private NoteEdit RenderEdit(Note note)
	{
		return new NoteEdit
		{
			Text      = note.Text,
			Cw        = note.Cw,
			FileIds   = note.FileIds,
			UpdatedAt = note.UpdatedAt ?? note.CreatedAt
		};
	}

	private static List<FilterResultEntity> GetFilterResult(
		IReadOnlyCollection<(Filter filter, string keyword)> filtered
	)
	{
		var res = new List<FilterResultEntity>();

		foreach (var entry in filtered)
		{
			var (filter, keyword) = entry;
			res.Add(new FilterResultEntity { Filter = FilterRenderer.RenderOne(filter), KeywordMatches = [keyword] });
		}

		return res;
	}

	private async Task<List<MentionEntity>> GetMentions(List<Note> notes)
	{
		if (notes.Count == 0) return [];
		var ids = notes.SelectMany(n => n.Mentions).Distinct();
		return await db.Users.IncludeCommonProperties()
		               .Where(p => ids.Contains(p.Id))
		               .Select(u => new MentionEntity(u, config.Value.WebDomain))
		               .ToListAsync();
	}

	private async Task<List<AttachmentEntity>> GetAttachments(List<Note> notes)
	{
		if (notes.Count == 0) return [];
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
			               Type        = AttachmentEntity.GetType(f.Type),
			               Sensitive   = f.IsSensitive
		               })
		               .ToListAsync();
	}

	private async Task<List<AttachmentEntity>> GetAttachments(IEnumerable<string> fileIds)
	{
		var ids = fileIds.Distinct().ToList();
		if (ids.Count == 0) return [];
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
			               Type        = AttachmentEntity.GetType(f.Type),
			               Sensitive   = f.IsSensitive
		               })
		               .ToListAsync();
	}

	internal async Task<List<AccountEntity>> GetAccounts(List<User> users)
	{
		if (users.Count == 0) return [];
		return (await userRenderer.RenderManyAsync(users.DistinctBy(p => p.Id))).ToList();
	}

	private async Task<List<string>> GetLikedNotes(List<Note> notes, User? user)
	{
		if (user == null) return [];
		if (notes.Count == 0) return [];
		return await db.NoteLikes.Where(p => p.User == user && notes.Contains(p.Note))
		               .Select(p => p.NoteId)
		               .ToListAsync();
	}

	private async Task<List<ReactionEntity>> GetReactions(List<Note> notes, User? user)
	{
		if (notes.Count == 0) return [];
		var counts = notes.ToDictionary(p => p.Id, p => p.Reactions);
		var res = await db.NoteReactions
		                  .Where(p => notes.Contains(p.Note))
		                  .GroupBy(p => new { p.NoteId, p.Reaction })
		                  .Select(p => new ReactionEntity
		                  {
			                  NoteId    = p.First().NoteId,
			                  Count     = (int)counts[p.First().NoteId].GetValueOrDefault(p.First().Reaction, 1),
			                  Name      = p.First().Reaction,
			                  Url       = null,
			                  StaticUrl = null,
			                  Me = user != null &&
			                       db.NoteReactions.Any(i => i.NoteId == p.First().NoteId &&
			                                                 i.Reaction == p.First().Reaction &&
			                                                 i.User == user)
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

	private async Task<List<string>> GetBookmarkedNotes(List<Note> notes, User? user)
	{
		if (user == null) return [];
		if (notes.Count == 0) return [];
		return await db.NoteBookmarks.Where(p => p.User == user && notes.Contains(p.Note))
		               .Select(p => p.NoteId)
		               .ToListAsync();
	}

	private async Task<List<string>> GetPinnedNotes(List<Note> notes, User? user)
	{
		if (user == null) return [];
		if (notes.Count == 0) return [];
		return await db.UserNotePins.Where(p => p.User == user && notes.Contains(p.Note))
		               .Select(p => p.NoteId)
		               .ToListAsync();
	}

	private async Task<List<string>> GetRenotes(List<Note> notes, User? user)
	{
		if (user == null) return [];
		if (notes.Count == 0) return [];
		return await db.Notes.Where(p => p.User == user && p.IsPureRenote && notes.Contains(p.Renote!))
		               .Select(p => p.RenoteId)
		               .Where(p => p != null)
		               .Distinct()
		               .Cast<string>()
		               .ToListAsync();
	}

	private async Task<List<PollEntity>> GetPolls(List<Note> notes, User? user)
	{
		if (notes.Count == 0) return [];
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
			               VisibleInPicker = true,
			               Category        = p.Category
		               })
		               .ToListAsync();
	}

	private async Task<List<Filter>> GetFilters(User? user, Filter.FilterContext? filterContext)
	{
		return filterContext == null
			? await db.Filters.Where(p => p.User == user).ToListAsync()
			: await db.Filters.Where(p => p.User == user && p.Contexts.Contains(filterContext.Value)).ToListAsync();
	}

	public async Task<IEnumerable<StatusEntity>> RenderManyAsync(
		IEnumerable<Note> notes, User? user, Filter.FilterContext? filterContext = null,
		List<AccountEntity>? accounts = null
	)
	{
		var noteList = notes.ToList();
		if (noteList.Count == 0) return [];

		var allNotes = noteList.SelectMany<Note, Note?>(p => [p, p.Renote, p.Renote?.Renote])
		                       .OfType<Note>()
		                       .Distinct()
		                       .ToList();

		var data = new NoteRendererDto
		{
			Accounts        = accounts ?? await GetAccounts(allNotes.Select(p => p.User).ToList()),
			Mentions        = await GetMentions(allNotes),
			Attachments     = await GetAttachments(allNotes),
			Polls           = await GetPolls(allNotes, user),
			LikedNotes      = await GetLikedNotes(allNotes, user),
			BookmarkedNotes = await GetBookmarkedNotes(allNotes, user),
			PinnedNotes     = await GetPinnedNotes(allNotes, user),
			Renotes         = await GetRenotes(allNotes, user),
			Emoji           = await GetEmoji(allNotes),
			Reactions       = await GetReactions(allNotes, user),
			Filters         = await GetFilters(user, filterContext)
		};

		return await noteList.Select(p => RenderAsync(p, user, filterContext, data)).AwaitAllAsync();
	}

	public class NoteRendererDto
	{
		public List<AccountEntity>?    Accounts;
		public List<AttachmentEntity>? Attachments;
		public List<string>?           BookmarkedNotes;
		public List<EmojiEntity>?      Emoji;
		public List<string>?           LikedNotes;
		public List<MentionEntity>?    Mentions;
		public List<string>?           PinnedNotes;
		public List<PollEntity>?       Polls;
		public List<ReactionEntity>?   Reactions;
		public List<string>?           Renotes;
		public List<Filter>?           Filters;

		public bool Source;
	}
}