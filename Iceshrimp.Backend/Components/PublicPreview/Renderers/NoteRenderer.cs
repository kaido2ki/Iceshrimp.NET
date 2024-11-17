using Iceshrimp.Backend.Components.PublicPreview.Schemas;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Components.PublicPreview.Renderers;

public class NoteRenderer(
	DatabaseContext db,
	UserRenderer userRenderer,
	MfmRenderer mfm,
	IOptions<Config.InstanceSection> instance,
	IOptionsSnapshot<Config.SecuritySection> security
) : IScopedService
{
	public async Task<PreviewNote?> RenderOne(Note? note)
	{
		if (note == null) return null;

		var allNotes = ((Note?[]) [note, note.Reply, note.Renote]).NotNull().ToList();

		var mentions    = await GetMentions(allNotes);
		var emoji       = await GetEmoji(allNotes);
		var users       = await GetUsers(allNotes);
		var attachments = await GetAttachments(allNotes);

		return await Render(note, users, mentions, emoji, attachments);
	}

	private async Task<PreviewNote> Render(
		Note note, List<PreviewUser> users, Dictionary<string, List<Note.MentionedUser>> mentions,
		Dictionary<string, List<Emoji>> emoji, Dictionary<string, List<PreviewAttachment>?> attachments
	)
	{
		var res = new PreviewNote
		{
			User              = users.First(p => p.Id == note.User.Id),
			Text              = await mfm.Render(note.Text, note.User.Host, mentions[note.Id], emoji[note.Id], "span"),
			Cw                = note.Cw,
			RawText           = note.Text,
			QuoteUrl          = note.Renote?.Url ?? note.Renote?.Uri ?? note.Renote?.GetPublicUriOrNull(instance.Value),
			QuoteInaccessible = note.Renote?.VisibilityIsPublicOrHome == false,
			Attachments       = attachments[note.Id],
			CreatedAt         = note.CreatedAt.ToDisplayStringTz(),
			UpdatedAt         = note.UpdatedAt?.ToDisplayStringTz()
		};

		return res;
	}

	private async Task<Dictionary<string, List<Note.MentionedUser>>> GetMentions(List<Note> notes)
	{
		var mentions = notes.SelectMany(n => n.Mentions).Distinct().ToList();
		if (mentions.Count == 0) return notes.ToDictionary<Note, string, List<Note.MentionedUser>>(p => p.Id, _ => []);

		var users = await db.Users.Where(p => mentions.Contains(p.Id))
		                    .ToDictionaryAsync(p => p.Id,
		                                       p => new Note.MentionedUser
		                                       {
			                                       Host     = p.Host,
			                                       Uri      = p.Uri ?? p.GetPublicUri(instance.Value),
			                                       Url      = p.UserProfile?.Url,
			                                       Username = p.Username
		                                       });
		return notes.ToDictionary(p => p.Id,
		                          p => users.Where(u => p.Mentions.Contains(u.Key)).Select(u => u.Value).ToList());
	}

	private async Task<Dictionary<string, List<Emoji>>> GetEmoji(List<Note> notes)
	{
		var ids = notes.SelectMany(n => n.Emojis).Distinct().ToList();
		if (ids.Count == 0) return notes.ToDictionary<Note, string, List<Emoji>>(p => p.Id, _ => []);

		var emoji = await db.Emojis.Where(p => ids.Contains(p.Id)).ToListAsync();
		return notes.ToDictionary(p => p.Id, p => emoji.Where(e => p.Emojis.Contains(e.Id)).ToList());
	}

	private async Task<List<PreviewUser>> GetUsers(List<Note> notes)
	{
		if (notes is []) return [];
		return await userRenderer.RenderMany(notes.Select(p => p.User).Distinct().ToList());
	}

	private async Task<Dictionary<string, List<PreviewAttachment>?>> GetAttachments(List<Note> notes)
	{
		if (security.Value.PublicPreview is Enums.PublicPreview.RestrictedNoMedia)
			return notes.ToDictionary<Note, string, List<PreviewAttachment>?>(p => p.Id,
			                                                                  p => p.FileIds is [] ? null : []);

		var ids   = notes.SelectMany(p => p.FileIds).ToList();
		var files = await db.DriveFiles.Where(p => ids.Contains(p.Id)).ToListAsync();
		return notes.ToDictionary<Note, string, List<PreviewAttachment>?>(p => p.Id,
		                                                                  p => files
		                                                                       .Where(f => p.FileIds.Contains(f.Id))
		                                                                       .Select(f => new PreviewAttachment
		                                                                       {
			                                                                       MimeType  = f.Type,
			                                                                       Url       = f.AccessUrl,
			                                                                       Name      = f.Name,
			                                                                       Alt       = f.Comment,
			                                                                       Sensitive = f.IsSensitive
		                                                                       })
		                                                                       .ToList());
	}

	public async Task<List<PreviewNote>> RenderMany(List<Note> notes)
	{
		if (notes is []) return [];
		var allNotes    = notes.SelectMany<Note, Note?>(p => [p, p.Renote, p.Reply]).NotNull().Distinct().ToList();
		var users       = await GetUsers(allNotes);
		var mentions    = await GetMentions(allNotes);
		var emoji       = await GetEmoji(allNotes);
		var attachments = await GetAttachments(allNotes);
		return await notes.Select(p => Render(p, users, mentions, emoji, attachments)).AwaitAllAsync().ToListAsync();
	}
}