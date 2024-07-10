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
[Route("/api/iceshrimp/profile")]
[Produces(MediaTypeNames.Application.Json)]
public class ProfileController(DatabaseContext db) : ControllerBase
{
	[HttpGet]
	[ProducesResults(HttpStatusCode.OK)]
	public UserProfileEntity GetProfile()
	{
		var user         = HttpContext.GetUserOrFail();
		var profile      = user.UserProfile ?? throw new Exception("Local user must have profile");
		var fields       = profile.Fields.Select(p => new UserProfileEntity.Field { Name = p.Name, Value = p.Value });
		var ffVisibility = (UserProfileEntity.FFVisibilityEnum)profile.FFVisibility;

		return new UserProfileEntity
		{
			Description  = profile.Description,
			Location     = profile.Location,
			Birthday     = profile.Birthday,
			FFVisibility = ffVisibility,
			Fields       = fields.ToList()
		};
	}

	[HttpPut]
	[Consumes(MediaTypeNames.Application.Json)]
	[ProducesResults(HttpStatusCode.OK)]
	public async Task UpdateSettings(UserProfileEntity newProfile)
	{
		var     user     = HttpContext.GetUserOrFail();
		var     profile  = user.UserProfile ?? throw new Exception("Local user must have profile");
		string? birthday = null;

		if (!string.IsNullOrWhiteSpace(newProfile.Birthday))
		{
			if (!DateOnly.TryParseExact(newProfile.Birthday, "O", out var parsed))
				throw GracefulException.BadRequest("Invalid birthday. Expected format is: YYYY-MM-DD");
			birthday = parsed.ToString("O");
		}

		var fields = newProfile.Fields.Select(p => new UserProfile.Field
		{
			Name       = p.Name,
			Value      = p.Value,
			IsVerified = false
		});

		profile.Description  = string.IsNullOrWhiteSpace(newProfile.Description) ? null : newProfile.Description;
		profile.Location     = string.IsNullOrWhiteSpace(newProfile.Location) ? null : newProfile.Location;
		profile.Birthday     = birthday;
		profile.Fields       = fields.ToArray();
		profile.FFVisibility = (UserProfile.UserProfileFFVisibility)newProfile.FFVisibility;

		await db.SaveChangesAsync();
	}
}