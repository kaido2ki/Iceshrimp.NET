using System.Net.Mime;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Shared.Schemas;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Iceshrimp.Backend.Controllers;

[ApiController]
[Authenticate]
[Authorize]
[EnableRateLimiting("sliding")]
[Route("/api/iceshrimp/settings")]
[Produces(MediaTypeNames.Application.Json)]
public class SettingsController(DatabaseContext db) : ControllerBase
{
	[HttpGet]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserSettingsEntity))]
	public async Task<IActionResult> GetSettings()
	{
		var settings = await GetOrInitUserSettings();

		var res = new UserSettingsEntity
		{
			FilterInaccessible      = settings.FilterInaccessible,
			PrivateMode             = settings.PrivateMode,
			DefaultNoteVisibility   = (NoteVisibility)settings.DefaultNoteVisibility,
			DefaultRenoteVisibility = (NoteVisibility)settings.DefaultNoteVisibility
		};

		return Ok(res);
	}

	[HttpPut]
	[Consumes(MediaTypeNames.Application.Json)]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(object))]
	public async Task<IActionResult> UpdateSettings(UserSettingsEntity newSettings)
	{
		var settings = await GetOrInitUserSettings();

		settings.FilterInaccessible      = newSettings.FilterInaccessible;
		settings.PrivateMode             = newSettings.PrivateMode;
		settings.DefaultNoteVisibility   = (Note.NoteVisibility)newSettings.DefaultNoteVisibility;
		settings.DefaultRenoteVisibility = (Note.NoteVisibility)newSettings.DefaultRenoteVisibility;

		return Ok(new object());
	}

	private async Task<UserSettings> GetOrInitUserSettings()
	{
		var user     = HttpContext.GetUserOrFail();
		var settings = user.UserSettings;
		if (settings == null)
		{
			settings = new UserSettings { User = user };
			db.Add(settings);
			await db.SaveChangesAsync();
			await db.ReloadEntityAsync(settings);
		}

		return settings;
	}
}