using System.Diagnostics.CodeAnalysis;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Helpers.LibMfm.Conversion;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Pages;

public class NoteModel(
	DatabaseContext db,
	IOptions<Config.InstanceSection> config,
	IOptions<Config.SecuritySection> security,
	MfmConverter mfmConverter
) : PageModel
{
	public Note?                               Note;
	public string?                             QuoteUrl;
	public Dictionary<string, string>          TextContent      = new();
	public Dictionary<string, List<DriveFile>> MediaAttachments = new();

	[SuppressMessage("ReSharper", "EntityFramework.NPlusOne.IncompleteDataQuery",
	                 Justification = "IncludeCommonProperties")]
	[SuppressMessage("ReSharper", "EntityFramework.NPlusOne.IncompleteDataUsage", Justification = "Same as above")]
	public async Task<IActionResult> OnGet(string id)
	{
		if (Request.Cookies.ContainsKey("session"))
			return Partial("Shared/FrontendSPA");

		if (!security.Value.EnablePublicPreview)
			throw new Exception("Public preview is disabled on this instance");
		
		//TODO: redirect to login (with route as url param) when public preview is disabled
		//TODO: login button
		//TODO: thread view
		//TODO: emoji

		Note = await db.Notes
		               .IncludeCommonProperties()
		               .Where(p => p.Id == id && p.VisibilityIsPublicOrHome)
		               .PrecomputeVisibilities(null)
		               .FirstOrDefaultAsync();

		QuoteUrl = Note?.Renote?.Url ?? Note?.Renote?.Uri ?? Note?.Renote?.GetPublicUriOrNull(config.Value);
		Note     = Note?.EnforceRenoteReplyVisibility();

		if (Note != null)
			MediaAttachments[Note.Id] = await GetAttachments(Note);

		if (Note is { IsPureRenote: true })
			return RedirectPermanent(Note.Renote?.Url ??
			                         Note.Renote?.Uri ??
			                         Note.Renote?.GetPublicUriOrNull(config.Value) ??
			                         throw new Exception("Note is remote but has no uri"));

		if (Note is { UserHost: not null })
			return RedirectPermanent(Note.Url ?? Note.Uri ?? throw new Exception("Note is remote but has no uri"));

		if (Note is { Text: not null })
		{
			TextContent[Note.Id] = await mfmConverter.ToHtmlAsync(Note.Text, await GetMentions(Note), Note.UserHost);
			if (Note.Renote is { Text: not null })
			{
				TextContent[Note.Renote.Id] = await mfmConverter.ToHtmlAsync(Note.Renote.Text,
				                                                             await GetMentions(Note.Renote),
				                                                             Note.Renote.UserHost);
				MediaAttachments[Note.Renote.Id] = await GetAttachments(Note.Renote);
			}
		}

		return Page();
	}

	private async Task<List<DriveFile>> GetAttachments(Note? note)
	{
		if (note == null || note.FileIds.Count == 0) return [];
		return await db.DriveFiles.Where(p => note.FileIds.Contains(p.Id)).ToListAsync();
	}

	private async Task<List<Note.MentionedUser>> GetMentions(Note note)
	{
		return await db.Users.IncludeCommonProperties()
		               .Where(p => note.Mentions.Contains(p.Id))
		               .Select(u => new Note.MentionedUser
		               {
			               Host     = u.Host ?? config.Value.AccountDomain,
			               Uri      = u.Uri ?? u.GetPublicUri(config.Value),
			               Username = u.Username,
			               Url      = u.UserProfile != null ? u.UserProfile.Url : null
		               })
		               .ToListAsync();
	}
}