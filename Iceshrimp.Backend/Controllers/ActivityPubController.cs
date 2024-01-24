using System.Net.Mime;
using Iceshrimp.Backend.Controllers.Attributes;
using Iceshrimp.Backend.Controllers.Renderers.ActivityPub;
using Iceshrimp.Backend.Controllers.Schemas;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Federation.ActivityStreams;
using Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Backend.Core.Queues;
using Iceshrimp.Backend.Core.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;

namespace Iceshrimp.Backend.Controllers;

[ApiController]
[UseNewtonsoftJson]
public class ActivityPubController(DatabaseContext db, UserRenderer userRenderer, NoteRenderer noteRenderer)
	: Controller {
	[HttpGet("/notes/{id}")]
	[AuthorizedFetch]
	[MediaTypeRouteFilter("application/activity+json", "application/ld+json")]
	[Produces("application/activity+json", "application/ld+json; profile=\"https://www.w3.org/ns/activitystreams\"")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ASNote))]
	[ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ErrorResponse))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
	public async Task<IActionResult> GetNote(string id) {
		var note = await db.Notes.FirstOrDefaultAsync(p => p.Id == id);
		if (note == null) return NotFound();
		var rendered  = noteRenderer.Render(note);
		var compacted = LdHelpers.Compact(rendered);
		return Ok(compacted);
	}

	[HttpGet("/users/{id}")]
	[AuthorizedFetch]
	[MediaTypeRouteFilter("application/activity+json", "application/ld+json")]
	[Produces("application/activity+json", "application/ld+json; profile=\"https://www.w3.org/ns/activitystreams\"")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ASActor))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
	public async Task<IActionResult> GetUser(string id) {
		var user = await db.Users.FirstOrDefaultAsync(p => p.Id == id);
		if (user == null) return NotFound();
		var rendered  = await userRenderer.Render(user);
		var compacted = LdHelpers.Compact(rendered);
		return Ok(compacted);
	}

	[HttpPost("/inbox")]
	[HttpPost("/users/{id}/inbox")]
	[AuthorizedFetch(true)]
	[EnableRequestBuffering(1024 * 1024)]
	[Produces("text/plain")]
	[Consumes(MediaTypeNames.Application.Json)]
	public IActionResult Inbox([FromBody] JToken body, string? id, [FromServices] QueueService queues) {
		queues.InboxQueue.Enqueue(new InboxJob(body, id));
		return Accepted();
	}
}