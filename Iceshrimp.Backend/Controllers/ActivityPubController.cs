using Iceshrimp.Backend.Controllers.Attributes;
using Iceshrimp.Backend.Controllers.Renderers.ActivityPub;
using Iceshrimp.Backend.Controllers.Schemas;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Federation.ActivityStreams;
using Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Controllers;

[ApiController]
[MediaTypeRouteFilter("application/activity+json", "application/ld+json")]
[Produces("application/activity+json", "application/ld+json")]
public class ActivityPubController : Controller {
	/*
	[HttpGet("/notes/{id}")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Note))]
	[ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ErrorResponse))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
	public async Task<IActionResult> GetNote(string id) {
		var db  = new DatabaseContext();
		var note = await db.Notes.FirstOrDefaultAsync(p => p.Id == id);
		if (note == null) return NotFound();
		return Ok(note);
	}
	*/

	[HttpGet("/users/{id}")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ASActor))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
	public async Task<IActionResult> GetUser(string id) {
		var db   = new DatabaseContext();
		var user = await db.Users.FirstOrDefaultAsync(p => p.Id == id);
		if (user == null) return NotFound();
		var rendered  = await ActivityPubUserRenderer.Render(user);
		var compacted = LDHelpers.Compact(rendered);
		return Ok(compacted);
	}
}