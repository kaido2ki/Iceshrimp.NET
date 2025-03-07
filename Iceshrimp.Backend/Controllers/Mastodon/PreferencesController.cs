using Iceshrimp.Backend.Controllers.Mastodon.Attributes;
using Iceshrimp.Backend.Controllers.Mastodon.Schemas.Entities;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Middleware;
using Microsoft.AspNetCore.Mvc;

namespace Iceshrimp.Backend.Controllers.Mastodon;

[Route("/api/v1/preferences")]
[Authenticate]
[MastodonApiController]
public class PreferencesController : ControllerBase
{
	[Authorize("read:accounts")]
	[HttpGet]
	public PreferencesEntity GetPreferences()
	{
		var settings   = HttpContext.GetUserOrFail().UserSettings;
		var visibility = StatusEntity.EncodeVisibility(settings?.DefaultNoteVisibility ?? Note.NoteVisibility.Public);

		return new PreferencesEntity
		{
			PostingDefaultVisibility = visibility,
			PostingDefaultSensitive  = settings?.AlwaysMarkSensitive ?? false,
			ReadingExpandMedia       = "default",
			ReadingExpandSpoilers    = false
		};
	}
}
