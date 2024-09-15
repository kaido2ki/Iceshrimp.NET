using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Mime;
using Iceshrimp.Backend.Controllers.Shared.Attributes;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Middleware;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Controllers.Razor;

[ApiController]
[Authenticate]
[EnableRateLimiting("sliding")]
[Produces(MediaTypeNames.Application.Json)]
public class RedirectController(IOptionsSnapshot<Config.SecuritySection> config, DatabaseContext db) : ControllerBase
{
	// @formatter:off
	[SuppressMessage("ReSharper", "EntityFramework.NPlusOne.IncompleteDataUsage", Justification = "IncludeCommonProperties")]
	[SuppressMessage("ReSharper", "EntityFramework.NPlusOne.IncompleteDataQuery", Justification = "IncludeCommonProperties")]
	// @formatter:on
	[HttpGet("/users/{id}")]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.Forbidden, HttpStatusCode.NotFound)]
	public async Task<IActionResult> GetUser(string id)
	{
		var localUser = HttpContext.GetUser();
		if (config.Value.PublicPreview == Enums.PublicPreview.Lockdown && localUser == null)
			throw GracefulException.Forbidden("Public preview is disabled on this instance");

		var user = await db.Users.IncludeCommonProperties().FirstOrDefaultAsync(p => p.Id == id) ??
		           throw GracefulException.NotFound("User not found");

		return user.IsLocalUser
			? Redirect($"/@{user.Username}")
			: Redirect(user.UserProfile?.Url ?? user.Uri ?? throw new Exception("Remote user must have an URI"));
	}
}