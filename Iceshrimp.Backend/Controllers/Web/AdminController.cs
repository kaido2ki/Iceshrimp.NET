using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Mime;
using Iceshrimp.Backend.Controllers.Federation;
using Iceshrimp.Backend.Controllers.Shared.Attributes;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Federation.ActivityStreams;
using Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;
using Iceshrimp.Backend.Core.Helpers;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Backend.Core.Services;
using Iceshrimp.Shared.Schemas.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;

namespace Iceshrimp.Backend.Controllers.Web;

[Authenticate]
[Authorize("role:admin")]
[ApiController]
[Route("/api/iceshrimp/admin")]
[SuppressMessage("ReSharper", "SuggestBaseTypeForParameterInConstructor",
                 Justification = "We only have a DatabaseContext in our DI pool, not the base type")]
public class AdminController(
	DatabaseContext db,
	ActivityPubController apController,
	ActivityPub.ActivityFetcherService fetchSvc,
	ActivityPub.NoteRenderer noteRenderer,
	ActivityPub.UserRenderer userRenderer,
	IOptions<Config.InstanceSection> config,
	QueueService queueSvc
) : ControllerBase
{
	[HttpPost("invites/generate")]
	[Produces(MediaTypeNames.Application.Json)]
	[ProducesResults(HttpStatusCode.OK)]
	public async Task<InviteResponse> GenerateInvite()
	{
		var invite = new RegistrationInvite
		{
			Id        = IdHelpers.GenerateSlowflakeId(),
			CreatedAt = DateTime.UtcNow,
			Code      = CryptographyHelpers.GenerateRandomString(32)
		};

		await db.AddAsync(invite);
		await db.SaveChangesAsync();

		return new InviteResponse { Code = invite.Code };
	}

	[HttpPost("users/{id}/reset-password")]
	[Produces(MediaTypeNames.Application.Json)]
	[Consumes(MediaTypeNames.Application.Json)]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.BadRequest, HttpStatusCode.NotFound)]
	public async Task ResetPassword(string id, [FromBody] ResetPasswordRequest request)
	{
		var settings = await db.UserSettings.FirstOrDefaultAsync(p => p.UserId == id) ??
		               throw GracefulException.RecordNotFound();

		if (request.Password.Length < 8)
			throw GracefulException.BadRequest("Password must be at least 8 characters long");

		settings.Password = AuthHelpers.HashPassword(request.Password);
		await db.SaveChangesAsync();
	}

	[HttpPost("instances/{host}/force-state/{state}")]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.NotFound)]
	public async Task ForceInstanceState(string host, AdminSchemas.InstanceState state)
	{
		var instance = await db.Instances.FirstOrDefaultAsync(p => p.Host == host.ToLowerInvariant()) ??
		               throw GracefulException.NotFound("Instance not found");

		if (state == AdminSchemas.InstanceState.Active)
		{
			instance.IsNotResponding         = false;
			instance.LastCommunicatedAt      = DateTime.UtcNow;
			instance.LatestRequestReceivedAt = DateTime.UtcNow;
			instance.LatestRequestSentAt     = DateTime.UtcNow;
		}
		else
		{
			instance.IsNotResponding         = true;
			instance.LastCommunicatedAt      = DateTime.UnixEpoch;
			instance.LatestRequestReceivedAt = DateTime.UnixEpoch;
			instance.LatestRequestSentAt     = DateTime.UnixEpoch;
		}

		await db.SaveChangesAsync();
	}

	[HttpPost("queue/jobs/{id::guid}/retry")]
	[Produces(MediaTypeNames.Application.Json)]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.BadRequest, HttpStatusCode.NotFound)]
	public async Task RetryQueueJob(Guid id)
	{
		var job = await db.Jobs.FirstOrDefaultAsync(p => p.Id == id) ??
		          throw GracefulException.NotFound($"Job {id} was not found.");

		await queueSvc.RetryJobAsync(job);
	}

	[UseNewtonsoftJson]
	[HttpGet("activities/notes/{id}")]
	[OverrideResultType<ASNote>]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.NotFound)]
	[Produces("application/activity+json", "application/ld+json; profile=\"https://www.w3.org/ns/activitystreams\"")]
	public async Task<JObject> GetNoteActivity(string id)
	{
		var note = await db.Notes
		                   .IncludeCommonProperties()
		                   .FirstOrDefaultAsync(p => p.Id == id && p.UserHost == null);
		if (note == null) throw GracefulException.NotFound("Note not found");
		var rendered = await noteRenderer.RenderAsync(note);
		return rendered.Compact() ?? throw new Exception("Failed to compact JSON-LD payload");
	}

	[UseNewtonsoftJson]
	[HttpGet("activities/notes/{id}/activity")]
	[OverrideResultType<ASAnnounce>]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.NotFound)]
	[ProducesActivityStreamsPayload]
	[SuppressMessage("ReSharper", "EntityFramework.NPlusOne.IncompleteDataUsage")]
	[SuppressMessage("ReSharper", "EntityFramework.NPlusOne.IncompleteDataQuery")]
	public async Task<JObject> GetRenoteActivity(string id)
	{
		var note = await db.Notes
		                   .IncludeCommonProperties()
		                   .Where(p => p.Id == id && p.UserHost == null && p.IsPureRenote && p.Renote != null)
		                   .FirstOrDefaultAsync() ??
		           throw GracefulException.NotFound("Note not found");

		return ActivityPub.ActivityRenderer
		                  .RenderAnnounce(noteRenderer.RenderLite(note.Renote!),
		                                  note.GetPublicUri(config.Value),
		                                  userRenderer.RenderLite(note.User),
		                                  note.Visibility,
		                                  note.User.GetPublicUri(config.Value) + "/followers")
		                  .Compact();
	}

	[UseNewtonsoftJson]
	[HttpGet("activities/users/{id}")]
	[OverrideResultType<ASActor>]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.NotFound)]
	[ProducesActivityStreamsPayload]
	public async Task<ActionResult<JObject>> GetUserActivity(string id)
	{
		return await apController.GetUser(id);
	}

	[UseNewtonsoftJson]
	[HttpGet("activities/users/{id}/collections/featured")]
	[OverrideResultType<ASOrderedCollection>]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesActivityStreamsPayload]
	public async Task<JObject> GetUserFeaturedActivity(string id)
	{
		return await apController.GetUserFeatured(id);
	}

	[UseNewtonsoftJson]
	[HttpGet("activities/users/@{acct}")]
	[OverrideResultType<ASActor>]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesActivityStreamsPayload]
	public async Task<ActionResult<JObject>> GetUserActivityByUsername(string acct)
	{
		return await apController.GetUserByUsername(acct);
	}

	[UseNewtonsoftJson]
	[HttpGet("activities/fetch")]
	[OverrideResultType<ASObject>]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesActivityStreamsPayload]
	public async Task<IActionResult> FetchActivityAsync([FromQuery] string uri, [FromQuery] string? userId)
	{
		var user     = userId != null ? await db.Users.FirstOrDefaultAsync(p => p.Id == userId && p.IsLocalUser) : null;
		var activity = await fetchSvc.FetchActivityAsync(uri, user);
		if (!activity.Any()) throw GracefulException.UnprocessableEntity("Failed to fetch activity");
		return Ok(LdHelpers.Compact(activity));
	}

	[UseNewtonsoftJson]
	[HttpGet("activities/fetch-raw")]
	[OverrideResultType<ASObject>]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.UnprocessableEntity)]
	[ProducesActivityStreamsPayload]
	public async Task FetchRawActivityAsync([FromQuery] string uri, [FromQuery] string? userId)
	{
		var user     = userId != null ? await db.Users.FirstOrDefaultAsync(p => p.Id == userId && p.IsLocalUser) : null;
		var activity = await fetchSvc.FetchRawActivityAsync(uri, user);
		if (activity == null) throw GracefulException.UnprocessableEntity("Failed to fetch activity");

		Response.ContentType = Request.Headers.Accept.Any(p => p != null && p.StartsWith("application/ld+json"))
			? "application/ld+json; profile=\"https://www.w3.org/ns/activitystreams\""
			: "application/activity+json";

		await Response.WriteAsync(activity);
	}
}