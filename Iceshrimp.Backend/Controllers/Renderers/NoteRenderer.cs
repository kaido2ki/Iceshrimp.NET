using Iceshrimp.Shared.Schemas;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Helpers;
using Iceshrimp.Backend.Core.Services;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Controllers.Renderers;

public class NoteRenderer(UserRenderer userRenderer, DatabaseContext db, EmojiService emojiSvc)
{
	public async Task<NoteResponse> RenderOne(
		Note note, User? user, Filter.FilterContext? filterContext = null, NoteRendererDto? data = null
	)
	{
		var res = await RenderBaseInternal(note, user, data);

		var renote = note is { Renote: not null, IsPureRenote: true }
			? await RenderRenote(note.Renote, user, data)
			: null;
		var quote = note is { Renote: not null, IsQuote: true } ? await RenderBase(note.Renote, user, data) : null;
		var reply = note.Reply != null ? await RenderBase(note.Reply, user, data) : null;

		var filters  = data?.Filters ?? await GetFilters(user, filterContext);
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
		res.ReplyInaccessible = note.ReplyUri != null;

		return res;
	}

	private async Task<NoteWithQuote> RenderRenote(Note note, User? user, NoteRendererDto? data = null)
	{
		var res   = await RenderBaseInternal(note, user, data);
		var quote = note.Renote is { IsPureRenote: false } ? await RenderBase(note.Renote, user, data) : null;

		res.Quote             = quote;
		res.QuoteId           = note.RenoteId;
		res.QuoteInaccessible = note.RenoteUri != null;

		return res;
	}

	private async Task<NoteBase> RenderBase(Note note, User? localUser, NoteRendererDto? data = null)
		=> await RenderBaseInternal(note, localUser, data);

	private async Task<NoteResponse> RenderBaseInternal(Note note, User? user, NoteRendererDto? data = null)
	{
		var noteUser    = (data?.Users ?? await GetUsers([note])).First(p => p.Id == note.User.Id);
		var attachments = (data?.Attachments ?? await GetAttachments([note])).Where(p => note.FileIds.Contains(p.Id));
		var reactions   = (data?.Reactions ?? await GetReactions([note], user)).Where(p => p.NoteId == note.Id);
		var liked = data?.LikedNotes?.Contains(note.Id) ??
		            await db.NoteLikes.AnyAsync(p => p.Note == note && p.User == user);

		return new NoteResponse
		{
			Id          = note.Id,
			CreatedAt   = note.CreatedAt.ToStringIso8601Like(),
			Text        = note.Text,
			Cw          = note.Cw,
			Visibility  = (NoteVisibility)note.Visibility,
			User        = noteUser,
			Attachments = attachments.ToList(),
			Reactions   = reactions.ToList(),
			Likes       = note.LikeCount,
			Renotes     = note.RenoteCount,
			Replies     = note.RepliesCount,
			Liked       = liked
		};
	}

	private async Task<List<UserResponse>> GetUsers(List<Note> notesList)
	{
		if (notesList.Count == 0) return [];
		var users = notesList.Select(p => p.User).DistinctBy(p => p.Id);
		return await userRenderer.RenderMany(users).ToListAsync();
	}

	private async Task<List<NoteAttachment>> GetAttachments(List<Note> notesList)
	{
		if (notesList.Count == 0) return [];
		var ids   = notesList.SelectMany(p => p.FileIds).Distinct();
		var files = await db.DriveFiles.Where(p => ids.Contains(p.Id)).ToListAsync();
		return files.Select(p => new NoteAttachment
		            {
			            Id           = p.Id,
			            Url          = p.PublicUrl,
			            ThumbnailUrl = p.PublicThumbnailUrl,
			            Blurhash     = p.Blurhash,
			            AltText      = p.Comment,
			            IsSensitive  = p.IsSensitive
		            })
		            .ToList();
	}

	private async Task<List<NoteReactionSchema>> GetReactions(List<Note> notes, User? user)
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
			                  Name = p.First().Reaction,
			                  Url  = null
		                  })
		                  .ToListAsync();

		foreach (var item in res.Where(item => item.Name.StartsWith(':')))
		{
			var hit = await emojiSvc.ResolveEmoji(item.Name);
			if (hit == null) continue;
			item.Url = hit.PublicUrl;
		}

		return res;
	}

	private async Task<List<string>> GetLikedNotes(List<Note> notes, User? user)
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

	private async Task<List<Filter>> GetFilters(User? user, Filter.FilterContext? filterContext)
	{
		if (filterContext == null) return [];
		return await db.Filters.Where(p => p.User == user && p.Contexts.Contains(filterContext.Value)).ToListAsync();
	}

	public async Task<IEnumerable<NoteResponse>> RenderMany(
		IEnumerable<Note> notes, User? user, Filter.FilterContext? filterContext = null
	)
	{
		var notesList = notes.ToList();
		if (notesList.Count == 0) return [];
		var allNotes = GetAllNotes(notesList);
		var data = new NoteRendererDto
		{
			Users       = await GetUsers(allNotes),
			Attachments = await GetAttachments(allNotes),
			Reactions   = await GetReactions(allNotes, user),
			Filters     = await GetFilters(user, filterContext),
			LikedNotes  = await GetLikedNotes(allNotes, user)
		};

		return await notesList.Select(p => RenderOne(p, user, filterContext, data)).AwaitAllAsync();
	}

	public class NoteRendererDto
	{
		public List<NoteAttachment>?     Attachments;
		public List<NoteReactionSchema>? Reactions;
		public List<UserResponse>?       Users;
		public List<Filter>?             Filters;
		public List<string>?             LikedNotes;
	}
}