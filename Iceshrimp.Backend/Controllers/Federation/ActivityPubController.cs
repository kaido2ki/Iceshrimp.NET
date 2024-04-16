using System.Text;
using Iceshrimp.Backend.Controllers.Attributes;
using Iceshrimp.Backend.Controllers.Federation.Attributes;
using Iceshrimp.Shared.Schemas;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Federation.ActivityStreams;
using Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Backend.Core.Queues;
using Iceshrimp.Backend.Core.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Controllers.Federation;

[FederationApiController]
[UseNewtonsoftJson]
[Produces("application/activity+json", "application/ld+json; profile=\"https://www.w3.org/ns/activitystreams\"")]
public class ActivityPubController(
	DatabaseContext db,
	QueueService queues,
	ActivityPub.NoteRenderer noteRenderer,
	ActivityPub.UserRenderer userRenderer,
	IOptions<Config.InstanceSection> config
) : ControllerBase
{
	[HttpGet("/notes/{id}")]
	[AuthorizedFetch]
	[MediaTypeRouteFilter("application/activity+json", "application/ld+json")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ASNote))]
	[ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ErrorResponse))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
	public async Task<IActionResult> GetNote(string id)
	{
		var actor = HttpContext.GetActor();
		var note = await db.Notes
		                   .IncludeCommonProperties()
		                   .EnsureVisibleFor(actor)
		                   .FirstOrDefaultAsync(p => p.Id == id && p.UserHost == null);
		if (note == null) return NotFound();
		var rendered  = await noteRenderer.RenderAsync(note);
		var compacted = LdHelpers.Compact(rendered);
		return Ok(compacted);
	}

	[HttpGet("/users/{id}")]
	[AuthorizedFetch]
	[MediaTypeRouteFilter("application/activity+json", "application/ld+json")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ASActor))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
	public async Task<IActionResult> GetUser(string id)
	{
		var user = await db.Users.IncludeCommonProperties().FirstOrDefaultAsync(p => p.Id == id);
		if (user == null) return NotFound();
		if (user.Host != null) return user.Uri != null ? RedirectPermanent(user.Uri) : NotFound();
		var rendered  = await userRenderer.RenderAsync(user);
		var compacted = LdHelpers.Compact(rendered);
		return Ok(compacted);
	}

	[HttpGet("/users/{id}/collections/featured")]
	[AuthorizedFetch]
	[MediaTypeRouteFilter("application/activity+json", "application/ld+json")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ASActor))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
	public async Task<IActionResult> GetUserFeatured(string id)
	{
		var user = await db.Users.IncludeCommonProperties().FirstOrDefaultAsync(p => p.Id == id && p.Host == null);
		if (user == null) return NotFound();

		var pins = await db.UserNotePins.Where(p => p.User == user)
		                   .OrderByDescending(p => p.Id)
		                   .Include(p => p.Note.User.UserProfile)
		                   .Include(p => p.Note.Renote!.User.UserProfile)
		                   .Include(p => p.Note.Reply!.User.UserProfile)
		                   .Select(p => p.Note)
		                   .Take(10)
		                   .ToListAsync();

		var rendered = pins.Select(noteRenderer.RenderLite).ToList();
		var res = new ASOrderedCollection
		{
			Id         = $"{user.GetPublicUri(config.Value)}/collections/featured",
			TotalItems = (ulong)rendered.Count,
			Items      = rendered.Cast<ASObject>().ToList()
		};

		var compacted = LdHelpers.Compact(res);
		return Ok(compacted);
	}

	[HttpGet("/@{acct}")]
	[AuthorizedFetch]
	[MediaTypeRouteFilter("application/activity+json", "application/ld+json")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ASActor))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
	public async Task<IActionResult> GetUserByUsername(string acct)
	{
		var split = acct.Split('@');
		if (acct.Split('@').Length > 2) return NotFound();
		if (split.Length == 2)
		{
			var remoteUser = await db.Users.IncludeCommonProperties()
			                         .FirstOrDefaultAsync(p => p.UsernameLower == split[0].ToLowerInvariant() &&
			                                                   p.Host == split[1].ToLowerInvariant().ToPunycode());
			return remoteUser?.Uri != null ? RedirectPermanent(remoteUser.Uri) : NotFound();
		}

		var user = await db.Users.IncludeCommonProperties()
		                   .FirstOrDefaultAsync(p => p.UsernameLower == acct.ToLowerInvariant() && p.Host == null);
		if (user == null) return NotFound();
		var rendered  = await userRenderer.RenderAsync(user);
		var compacted = LdHelpers.Compact(rendered);
		return Ok(compacted);
	}

	[HttpPost("/inbox")]
	[HttpPost("/users/{id}/inbox")]
	[InboxValidation]
	[EnableRequestBuffering(1024 * 1024)]
	[Produces("text/plain")]
	[Consumes("application/activity+json", "application/ld+json; profile=\"https://www.w3.org/ns/activitystreams\"")]
	public async Task<IActionResult> Inbox(string? id)
	{
		using var reader = new StreamReader(Request.Body, Encoding.UTF8, true, 1024, true);
		var       body   = await reader.ReadToEndAsync();
		Request.Body.Position = 0;
		await queues.InboxQueue.EnqueueAsync(new InboxJobData
		{
			Body                = body,
			InboxUserId         = id,
			AuthenticatedUserId = HttpContext.GetActor()?.Id
		});
		return Accepted();
	}
}