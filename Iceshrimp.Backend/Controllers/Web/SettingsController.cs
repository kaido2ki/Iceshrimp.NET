using System.Net;
using System.Net.Mime;
using AngleSharp.Text;
using Iceshrimp.Backend.Controllers.Shared.Attributes;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Backend.Core.Services;
using Iceshrimp.Shared.Schemas.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Controllers.Web;

[ApiController]
[Authenticate]
[Authorize]
[EnableRateLimiting("sliding")]
[Route("/api/iceshrimp/settings")]
[Produces(MediaTypeNames.Application.Json)]
public class SettingsController(DatabaseContext db, UserService userSvc) : ControllerBase
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
			DefaultRenoteVisibility = (NoteVisibility)settings.DefaultNoteVisibility
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
	
	[HttpPost("export/following")]
	[ProducesResults(HttpStatusCode.Accepted)]
	[ProducesErrors(HttpStatusCode.BadRequest)]
	public async Task<AcceptedResult> ExportFollowing()
	{
		var user = HttpContext.GetUserOrFail();

		var followCount = await db.Followings
		                          .CountAsync(p => p.FollowerId == user.Id);
		if (followCount < 1)
			throw GracefulException.BadRequest("You do not follow any users");

		await userSvc.ExportFollowingAsync(user);
		
		return Accepted();
	}

	[HttpPost("import/following")]
	[ProducesResults(HttpStatusCode.Accepted)]
	public async Task<AcceptedResult> ImportFollowing(IFormFile file)
	{
		var user = HttpContext.GetUserOrFail();
		
		var reader   = new StreamReader(file.OpenReadStream());
		var contents = await reader.ReadToEndAsync();

		var fqns = contents
		           .Split("\n")
		           .Where(line => !string.IsNullOrWhiteSpace(line))
		           .Select(line => line.SplitCommas().First())
		           .Where(fqn => fqn.Contains('@'))
		           .ToList();

		await userSvc.ImportFollowingAsync(user, fqns);

		return Accepted();
	}
}