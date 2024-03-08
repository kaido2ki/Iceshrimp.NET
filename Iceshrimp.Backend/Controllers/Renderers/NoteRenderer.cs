using Iceshrimp.Backend.Controllers.Schemas;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Controllers.Renderers;

public class NoteRenderer(UserRenderer userRenderer, DatabaseContext db)
{
	public async Task<NoteResponse> RenderOne(Note note, NoteRendererDto? data = null)
	{
		var user        = (data?.Users ?? await GetUsers([note])).First(p => p.Id == note.User.Id);
		var attachments = (data?.Attachments ?? await GetAttachments([note])).Where(p => note.FileIds.Contains(p.Id));

		return new NoteResponse
		{
			Id          = note.Id,
			CreatedAt	= note.CreatedAt.ToStringIso8601Like(),
			Text        = note.Text,
			Cw          = note.Cw,
			Visibility  = RenderVisibility(note.Visibility),
			User        = user,
			Attachments = attachments.ToList()
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

	public async Task<IEnumerable<NoteResponse>> RenderMany(IEnumerable<Note> notes)
	{
		var notesList = notes.ToList();
		var data = new NoteRendererDto
		{
			Users = await GetUsers(notesList), Attachments = await GetAttachments(notesList)
		};

		return await notesList.Select(p => RenderOne(p, data)).AwaitAllAsync();
	}

	public class NoteRendererDto
	{
		public List<UserResponse>?   Users;
		public List<NoteAttachment>? Attachments;
	}
}