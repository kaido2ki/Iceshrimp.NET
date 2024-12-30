using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Helpers;
using Iceshrimp.Backend.Core.Services;
using Iceshrimp.Shared.Schemas.Web;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Controllers.Web.Renderers;

public class NoteRenderer(
	UserRenderer userRenderer,
	DatabaseContext db,
	EmojiService emojiSvc,
	MediaProxyService mediaProxy,
	IOptions<Config.InstanceSection> config
) : IScopedService
{
	public async Task<NoteResponse> RenderOne(
		Note note, User? user, Filter.FilterContext? filterContext = null, NoteRendererDto? data = null
	)
	{
		var res = await RenderBaseInternalAsync(note, user, data);

		var renote = note is { Renote: not null, IsPureRenote: true }
			? await RenderRenoteAsync(note.Renote, user, data)
			: null;
		var quote = note is { Renote: not null, IsQuote: true } ? await RenderBaseAsync(note.Renote, user, data) : null;
		var reply = note.Reply != null ? await RenderBaseAsync(note.Reply, user, data) : null;

		var filters  = data?.Filters ?? await GetFiltersAsync(user, filterContext);
		var filtered = FilterHelper.IsFiltered([note, note.Reply, note.Renote, note.Renote?.Renote], filters);

		if (filtered.HasValue)
		{
			res.Filtered = new NoteFilteredSchema
			{
				Id      = filtered.Value.filter.Id,
				Keyword = filtered.Value.keyword,
				Hide    = filtered.Value.filter.Action == Filter.FilterAction.Hide
			};
		}

		res.Renote            = renote;
		res.RenoteId          = note.IsPureRenote ? note.RenoteId : null;
		res.Quote             = quote;
		res.QuoteId           = note.IsQuote ? note.RenoteId : null;
		res.QuoteInaccessible = note.RenoteUri != null;
		res.Reply             = reply;
		res.ReplyId           = note.ReplyId;
		res.ReplyInaccessible = note.Reply == null && (note.ReplyId != null || note.ReplyUri != null);

		return res;
	}

	private async Task<NoteWithQuote> RenderRenoteAsync(Note note, User? user, NoteRendererDto? data = null)
	{
		var res   = await RenderBaseInternalAsync(note, user, data);
		var quote = note.Renote is { IsPureRenote: false } ? await RenderBaseAsync(note.Renote, user, data) : null;

		res.Quote             = quote;
		res.QuoteId           = note.RenoteId;
		res.QuoteInaccessible = note.Renote == null && (note.ReplyId != null || note.RenoteUri != null);

		return res;
	}

	private async Task<NoteBase> RenderBaseAsync(Note note, User? localUser, NoteRendererDto? data = null)
		=> await RenderBaseInternalAsync(note, localUser, data);

	private async Task<NoteResponse> RenderBaseInternalAsync(Note note, User? user, NoteRendererDto? data = null)
	{
		var noteUser = (data?.Users ?? await GetUsersAsync([note])).First(p => p.Id == note.User.Id);
		var attachments =
			(data?.Attachments ?? await GetAttachmentsAsync([note])).Where(p => note.FileIds.Contains(p.Id));
		var reactions = (data?.Reactions ?? await GetReactionsAsync([note], user)).Where(p => p.NoteId == note.Id);
		var liked = data?.LikedNotes?.Contains(note.Id) ??
		            await db.NoteLikes.AnyAsync(p => p.Note == note && p.User == user);
		var emoji = data?.Emoji?.Where(p => note.Emojis.Contains(p.Id)).ToList() ?? await GetEmojiAsync([note]);
		var poll  = (data?.Polls ?? await GetPollsAsync([note], user)).First(p => p.NoteId == note.Id);

		return new NoteResponse
		{
			Id          = note.Id,
			CreatedAt   = note.CreatedAt.ToStringIso8601Like(),
			Uri         = note.Uri ?? note.GetPublicUri(config.Value),
			Url         = note.Url ?? note.Uri ?? note.GetPublicUri(config.Value),
			Text        = note.Text,
			Cw          = note.Cw,
			Visibility  = (NoteVisibility)note.Visibility,
			User        = noteUser,
			Attachments = attachments.ToList(),
			Reactions   = reactions.ToList(),
			Likes       = note.LikeCount,
			Renotes     = note.RenoteCount,
			Replies     = note.RepliesCount,
			Liked       = liked,
			Emoji       = emoji,
			Poll        = poll
		};
	}

	private async Task<List<UserResponse>> GetUsersAsync(List<Note> notesList)
	{
		if (notesList.Count == 0) return [];
		var users = notesList.Select(p => p.User).DistinctBy(p => p.Id);
		return await userRenderer.RenderManyAsync(users).ToListAsync();
	}

	private async Task<List<NoteAttachment>> GetAttachmentsAsync(List<Note> notesList)
	{
		if (notesList.Count == 0) return [];
		var ids   = notesList.SelectMany(p => p.FileIds).Distinct();
		var files = await db.DriveFiles.Where(p => ids.Contains(p.Id)).ToListAsync();
		return files.Select(p => new NoteAttachment
		            {
			            Id           = p.Id,
			            Url          = mediaProxy.GetProxyUrl(p),
			            ThumbnailUrl = mediaProxy.GetThumbnailProxyUrl(p),
			            ContentType  = p.Type,
			            Blurhash     = p.Blurhash,
			            AltText      = p.Comment,
			            IsSensitive  = p.IsSensitive
		            })
		            .ToList();
	}

	private async Task<List<NoteReactionSchema>> GetReactionsAsync(List<Note> notes, User? user)
	{
		if (user == null) return [];
		if (notes.Count == 0) return [];
		var counts = notes.ToDictionary(p => p.Id, p => p.Reactions);
		var res = await db.NoteReactions
		                  .Where(p => notes.Contains(p.Note))
		                  .GroupBy(p => new { p.NoteId, p.Reaction })
		                  .Select(p => new NoteReactionSchema
		                  {
			                  NoteId = p.First().NoteId,
			                  Count  = (int)counts[p.First().NoteId].GetValueOrDefault(p.First().Reaction, 1),
			                  Reacted = db.NoteReactions.Any(i => i.NoteId == p.First().NoteId &&
			                                                      i.Reaction == p.First().Reaction &&
			                                                      i.User == user),
			                  Name      = p.First().Reaction,
			                  Url       = null,
			                  Sensitive = false
		                  })
		                  .ToListAsync();

		foreach (var item in res.Where(item => item.Name.StartsWith(':')))
		{
			var hit = await emojiSvc.ResolveEmojiAsync(item.Name);
			if (hit == null) continue;
			item.Url       = hit.GetAccessUrl(config.Value);
			item.Sensitive = hit.Sensitive;
		}

		return res;
	}

	private async Task<List<string>> GetLikedNotesAsync(List<Note> notes, User? user)
	{
		if (user == null) return [];
		if (notes.Count == 0) return [];
		return await db.NoteLikes.Where(p => p.User == user && notes.Contains(p.Note))
		               .Select(p => p.NoteId)
		               .ToListAsync();
	}

	private static List<Note> GetAllNotes(IEnumerable<Note> notes)
	{
		return notes.SelectMany<Note, Note?>(p => [p, p.Reply, p.Renote, p.Renote?.Renote])
		            .OfType<Note>()
		            .Distinct()
		            .ToList();
	}

	private async Task<List<Filter>> GetFiltersAsync(User? user, Filter.FilterContext? filterContext)
	{
		if (filterContext == null) return [];
		return await db.Filters.Where(p => p.User == user && p.Contexts.Contains(filterContext.Value)).ToListAsync();
	}

	private async Task<List<EmojiResponse>> GetEmojiAsync(IEnumerable<Note> notes)
	{
		var ids = notes.SelectMany(p => p.Emojis).ToList();
		if (ids.Count == 0) return [];

		return await db.Emojis
		               .Where(p => ids.Contains(p.Id))
		               .Select(p => new EmojiResponse
		               {
			               Id        = p.Id,
			               Name      = p.Name,
			               Uri       = p.Uri,
			               Aliases   = p.Aliases,
			               Category  = p.Category,
			               PublicUrl = p.GetAccessUrl(config.Value),
			               License   = p.License,
			               Sensitive = p.Sensitive
		               })
		               .ToListAsync();
	}

	private async Task<List<NotePollSchema>> GetPollsAsync(IEnumerable<Note> notes, User? user)
	{
		var polls = await db.Polls
		               .Where(p => notes.Contains(p.Note))
		               .ToListAsync();

		return polls
		       .Select(p => new NotePollSchema
		       {
			       NoteId    = p.NoteId,
			       ExpiresAt = p.ExpiresAt,
			       Multiple  = p.Multiple,
			       Choices = p.Choices.Zip(p.Votes)
			                  .Select((c, i) => new NotePollChoice
			                  {
				                  Value = c.First,
				                  Votes = c.Second,
				                  Voted = user != null
				                          && db.PollVotes.Any(v => v.NoteId == p.NoteId
				                                                   && v.Choice == i
				                                                   && v.User == user)
			                  })
			                  .ToList(),
			       VotersCount = p.VotersCount
		       })
		       .ToList();
	}

	public async Task<IEnumerable<NoteResponse>> RenderManyAsync(
		IEnumerable<Note> notes, User? user, Filter.FilterContext? filterContext = null
	)
	{
		var notesList = notes.ToList();
		if (notesList.Count == 0) return [];
		var allNotes = GetAllNotes(notesList);
		var data = new NoteRendererDto
		{
			Users       = await GetUsersAsync(allNotes),
			Attachments = await GetAttachmentsAsync(allNotes),
			Reactions   = await GetReactionsAsync(allNotes, user),
			Filters     = await GetFiltersAsync(user, filterContext),
			LikedNotes  = await GetLikedNotesAsync(allNotes, user),
			Emoji       = await GetEmojiAsync(allNotes),
			Polls       = await GetPollsAsync(allNotes, user)
		};

		return await notesList.Select(p => RenderOne(p, user, filterContext, data)).AwaitAllAsync();
	}

	public class NoteRendererDto
	{
		public List<NoteAttachment>?     Attachments;
		public List<EmojiResponse>?      Emoji;
		public List<Filter>?             Filters;
		public List<string>?             LikedNotes;
		public List<NoteReactionSchema>? Reactions;
		public List<UserResponse>?       Users;
		public List<NotePollSchema>?     Polls;
	}
}
