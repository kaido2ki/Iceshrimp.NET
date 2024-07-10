using System.Net;
using System.Net.Mime;
using Iceshrimp.Backend.Controllers.Shared.Attributes;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Shared.Schemas.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Iceshrimp.Backend.Controllers.Web;

[ApiController]
[Authenticate]
[Authorize]
[EnableRateLimiting("sliding")]
[Route("/api/iceshrimp/settings")]
[Produces(MediaTypeNames.Application.Json)]
public class SettingsController(DatabaseContext db) : ControllerBase
{
	[HttpGet]
	[ProducesResults(HttpStatusCode.OK)]
	public async Task<UserSettingsEntity> GetSettings()
	{
		var settings = await GetOrInitUserSettings();
		return new UserSettingsEntity
		{
			FilterInaccessible      = settings.FilterInaccessible,
			PrivateMode             = settings.PrivateMode,
			AlwaysMarkSensitive     = settings.AlwaysMarkSensitive,
			AutoAcceptFollowed      = settings.AutoAcceptFollowed,
			DefaultNoteVisibility   = (NoteVisibility)settings.DefaultNoteVisibility,
			DefaultRenoteVisibility = (NoteVisibility)settings.DefaultNoteVisibility,
		};
	}

	[HttpPut]
	[Consumes(MediaTypeNames.Application.Json)]
	[ProducesResults(HttpStatusCode.OK)]
	public async Task UpdateSettings(UserSettingsEntity newSettings)
	{
		var settings = await GetOrInitUserSettings();

		settings.FilterInaccessible      = newSettings.FilterInaccessible;
		settings.PrivateMode             = newSettings.PrivateMode;
		settings.AlwaysMarkSensitive     = newSettings.AlwaysMarkSensitive;
		settings.AutoAcceptFollowed      = newSettings.AutoAcceptFollowed;
		settings.DefaultNoteVisibility   = (Note.NoteVisibility)newSettings.DefaultNoteVisibility;
		settings.DefaultRenoteVisibility = (Note.NoteVisibility)newSettings.DefaultRenoteVisibility;

		await db.SaveChangesAsync();
	}

	private async Task<UserSettings> GetOrInitUserSettings()
	{
		var user     = HttpContext.GetUserOrFail();
		var settings = user.UserSettings;
		if (settings != null) return settings;

		settings = new UserSettings { User = user };
		db.Add(settings);
		await db.SaveChangesAsync();
		await db.ReloadEntityAsync(settings);
		return settings;
	}
}