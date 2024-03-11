using Iceshrimp.Backend.Controllers.Schemas;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Services;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Controllers.Renderers;

public class NoteRenderer(UserRenderer userRenderer, DatabaseContext db, EmojiService emojiSvc)
{
	public async Task<NoteResponse> RenderOne(Note note, User? localUser, NoteRendererDto? data = null)
	{
		var user        = (data?.Users ?? await GetUsers([note])).First(p => p.Id == note.User.Id);
		var attachments = (data?.Attachments ?? await GetAttachments([note])).Where(p => note.FileIds.Contains(p.Id));
		var reactions   = (data?.Reactions ?? await GetReactions([note], localUser)).Where(p => p.NoteId == note.Id);

		return new NoteResponse
		{
			Id          = note.Id,
			CreatedAt   = note.CreatedAt.ToStringIso8601Like(),
			Text        = note.Text,
			Cw          = note.Cw,
			Visibility  = RenderVisibility(note.Visibility),
			User        = user,
			Attachments = attachments.ToList(),
			Reactions   = reactions.ToList()
		};
	}

	private static string RenderVisibility(Note.NoteVisibility visibility) => visibility switch
	{
		Note.NoteVisibility.Public    => "public",
		Note.NoteVisibility.Home      => "home",
		Note.NoteVisibility.Followers => "followers",
		Note.NoteVisibility.Specified => "specified",
		_                             => throw new ArgumentOutOfRangeException(nameof(visibility), visibility, null)
	};

	private async Task<List<UserResponse>> GetUsers(IEnumerable<Note> notesList)
	{
		var users = notesList.Select(p => p.User).DistinctBy(p => p.Id);
		return await userRenderer.RenderMany(users).ToListAsync();
	}

	private async Task<List<NoteAttachment>> GetAttachments(IEnumerable<Note> notesList)
	{
		var ids   = notesList.SelectMany(p => p.FileIds).Distinct();
		var files = await db.DriveFiles.Where(p => ids.Contains(p.Id)).ToListAsync();
		return files.Select(p => new NoteAttachment
		            {
			            Id           = p.Id,
			            Url          = p.PublicUrl,
			            ThumbnailUrl = p.PublicThumbnailUrl,
			            Blurhash     = p.Blurhash,
			            AltText      = p.Comment
		            })
		            .ToList();
	}

	private async Task<List<NoteReactionSchema>> GetReactions(List<Note> notes, User? user)
	{
		if (user == null) return [];
		var counts = notes.ToDictionary(p => p.Id, p => p.Reactions);
		var res = await db.NoteReactions
		                  .Where(p => notes.Contains(p.Note))
		                  .GroupBy(p => p.Reaction)
		                  .Select(p => new NoteReactionSchema
		                  {
			                  NoteId = p.First().NoteId,
			                  Count  = (int)counts[p.First().NoteId].GetValueOrDefault(p.First().Reaction, 1),
			                  Reacted = db.NoteReactions.Any(i => i.NoteId == p.First().NoteId &&
			                                                      i.Reaction == p.First().Reaction &&
			                                                      i.User == user),
			                  Name = p.First().Reaction,
			                  Url  = null,
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

	public async Task<IEnumerable<NoteResponse>> RenderMany(IEnumerable<Note> notes, User? user)
	{
		var notesList = notes.ToList();
		var data = new NoteRendererDto
		{
			Users = await GetUsers(notesList), Attachments = await GetAttachments(notesList)
		};

		return await notesList.Select(p => RenderOne(p, user, data)).AwaitAllAsync();
	}

	public class NoteRendererDto
	{
		public List<UserResponse>?       Users;
		public List<NoteAttachment>?     Attachments;
		public List<NoteReactionSchema>? Reactions;
	}
}