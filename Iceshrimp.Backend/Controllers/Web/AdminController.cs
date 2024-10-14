using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Mime;
using System.Text.Json;
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
using Iceshrimp.Backend.Core.Tasks;
using Iceshrimp.Shared.Configuration;
using Iceshrimp.Shared.Schemas.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using static Iceshrimp.Backend.Core.Extensions.SwaggerGenOptionsExtensions;

namespace Iceshrimp.Backend.Controllers.Web;

[Authenticate]
[Authorize("role:admin")]
[ApiController]
[Route("/api/iceshrimp/admin")]
public class AdminController(
	DatabaseContext db,
	ActivityPubController apController,
	ActivityPub.ActivityFetcherService fetchSvc,
	ActivityPub.NoteRenderer noteRenderer,
	ActivityPub.UserRenderer userRenderer,
	IOptions<Config.InstanceSection> config,
	[SuppressMessage("ReSharper", "ParameterOnlyUsedForPreconditionCheck.Local")]
	IOptionsSnapshot<Config.SecuritySection> security,
	QueueService queueSvc,
	RelayService relaySvc,
	PolicyService policySvc
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

	[HttpGet("instances/allowed")]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.BadRequest)]
	public async Task<List<AllowedInstance>> GetAllowedInstances([FromQuery] int? limit, [FromQuery] int? offset)
	{
		if (security.Value.FederationMode == Enums.FederationMode.BlockList)
			throw GracefulException.BadRequest("Federation mode is set to blocklist.");

		var q = db.AllowedInstances.OrderBy(p => p.Host).AsQueryable();
		if (offset != null)
			q = q.Skip(offset.Value);
		if (limit != null)
			q = q.Take(limit.Value);

		return await q.ToListAsync();
	}

	[HttpGet("instances/blocked")]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.BadRequest)]
	public async Task<List<BlockedInstance>> GetBlockedInstances([FromQuery] int? limit, [FromQuery] int? offset)
	{
		if (security.Value.FederationMode == Enums.FederationMode.AllowList)
			throw GracefulException.BadRequest("Federation mode is set to allowlist.");

		var q = db.BlockedInstances.OrderBy(p => p.Host).AsQueryable();
		if (offset != null)
			q = q.Skip(offset.Value);
		if (limit != null)
			q = q.Take(limit.Value);

		return await q.ToListAsync();
	}

	[HttpPost("instances/{host}/allow")]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.BadRequest)]
	public async Task AllowInstance(string host, [FromQuery] bool? imported = false)
	{
		if (security.Value.FederationMode == Enums.FederationMode.BlockList)
			throw GracefulException.BadRequest("Federation mode is set to blocklist.");

		if (await db.AllowedInstances.FirstOrDefaultAsync(p => p.Host == host.ToPunycodeLower()) is { } instance)
		{
			if (imported.HasValue)
			{
				instance.IsImported = imported.Value;
				await db.SaveChangesAsync();
			}

			return;
		}

		var obj = new AllowedInstance { Host = host.ToPunycodeLower(), IsImported = imported ?? false };
		db.Add(obj);
		await db.SaveChangesAsync();
	}

	[HttpPost("instances/{host}/block")]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.BadRequest)]
	public async Task BlockInstance(string host, [FromQuery] bool? imported = null, [FromQuery] string? reason = null)
	{
		if (security.Value.FederationMode == Enums.FederationMode.AllowList)
			throw GracefulException.BadRequest("Federation mode is set to allowlist.");

		if (await db.BlockedInstances.FirstOrDefaultAsync(p => p.Host == host.ToPunycodeLower()) is { } instance)
		{
			if (imported.HasValue)
				instance.IsImported = imported.Value;
			if (reason != null)
				instance.Reason = reason;
			await db.SaveChangesAsync();
			return;
		}

		var obj = new BlockedInstance
		{
			Host       = host.ToPunycodeLower(),
			IsImported = imported ?? false,
			Reason     = reason
		};

		db.Add(obj);
		await db.SaveChangesAsync();
	}

	[HttpPost("instances/{host}/disallow")]
	[ProducesResults(HttpStatusCode.OK)]
	public async Task DisallowInstance(string host)
	{
		if (security.Value.FederationMode == Enums.FederationMode.BlockList)
			throw GracefulException.BadRequest("Federation mode is set to blocklist.");

		await db.AllowedInstances.Where(p => p.Host == host.ToPunycodeLower()).ExecuteDeleteAsync();
	}

	[HttpPost("instances/{host}/unblock")]
	[ProducesResults(HttpStatusCode.OK)]
	public async Task UnblockInstance(string host)
	{
		if (security.Value.FederationMode == Enums.FederationMode.AllowList)
			throw GracefulException.BadRequest("Federation mode is set to allowlist.");

		await db.BlockedInstances.Where(p => p.Host == host.ToPunycodeLower()).ExecuteDeleteAsync();
	}

	[HttpPost("instances/{host}/force-state/{state}")]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.NotFound)]
	public async Task ForceInstanceState(string host, AdminSchemas.InstanceState state)
	{
		var instance = await db.Instances.FirstOrDefaultAsync(p => p.Host == host.ToPunycodeLower()) ??
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
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.BadRequest, HttpStatusCode.NotFound)]
	public async Task RetryQueueJob(Guid id)
	{
		var job = await db.Jobs.FirstOrDefaultAsync(p => p.Id == id) ??
		          throw GracefulException.NotFound($"Job {id} was not found.");

		await queueSvc.RetryJobAsync(job);
	}

	[HttpPost("queue/{queue}/retry-all")]
	[ProducesResults(HttpStatusCode.OK)]
	public async Task RetryFailedJobs(string queue)
	{
		var jobs = db.Jobs
		             .Where(p => p.Queue == queue && p.Status == Job.JobStatus.Failed)
		             .AsChunkedAsyncEnumerable(10, p => p.Id);

		await foreach (var job in jobs)
			await queueSvc.RetryJobAsync(job);
	}

	[HttpPost("queue/{queue}/retry-range/{from::guid}/{to::guid}")]
	[ProducesResults(HttpStatusCode.OK)]
	public async Task RetryRange(string queue, Guid from, Guid to)
	{
		var jobs = db.Jobs
		             .Where(p => p.Queue == queue && p.Status == Job.JobStatus.Failed)
		             .Where(p => p.Id >= from && p.Id <= to)
		             .AsChunkedAsyncEnumerable(10, p => p.Id);

		await foreach (var job in jobs)
			await queueSvc.RetryJobAsync(job);
	}

	[HttpPost("queue/jobs/{id::guid}/abandon")]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.BadRequest, HttpStatusCode.NotFound)]
	public async Task AbandonQueueJob(Guid id)
	{
		var job = await db.Jobs.FirstOrDefaultAsync(p => p.Id == id) ??
		          throw GracefulException.NotFound($"Job {id} was not found.");

		await queueSvc.AbandonJobAsync(job);
	}

	[HttpGet("relays")]
	[ProducesResults(HttpStatusCode.OK)]
	public async Task<List<RelaySchemas.RelayResponse>> GetRelays()
	{
		return await db.Relays
		               .ToArrayAsync()
		               .ContinueWithResult(res => res.Select(p => new RelaySchemas.RelayResponse
		                                             {
			                                             Id     = p.Id,
			                                             Inbox  = p.Inbox,
			                                             Status = (RelaySchemas.RelayStatus)p.Status
		                                             })
		                                             .ToList());
	}

	[HttpPost("relays")]
	[ProducesResults(HttpStatusCode.OK)]
	public async Task SubscribeToRelay(RelaySchemas.RelayRequest rq)
	{
		await relaySvc.SubscribeToRelay(rq.Inbox);
	}

	[HttpDelete("relays/{id}")]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.NotFound)]
	public async Task UnsubscribeFromRelay(string id)
	{
		var relay = await db.Relays.FirstOrDefaultAsync(p => p.Id == id) ??
		            throw GracefulException.NotFound("Relay not found");
		await relaySvc.UnsubscribeFromRelay(relay);
	}

	[HttpPost("drive/prune-expired-media")]
	[ProducesResults(HttpStatusCode.OK)]
	public async Task PruneExpiredMedia([FromServices] IServiceScopeFactory factory)
	{
		await using var scope = factory.CreateAsyncScope();
		await new MediaCleanupTask().Invoke(scope.ServiceProvider);
	}

	[HttpGet("policy")]
	[ProducesResults(HttpStatusCode.OK)]
	public async Task<List<string>> GetAvailablePolicies() => await policySvc.GetAvailablePolicies();

	[HttpGet("policy/{name}")]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.NotFound)]
	public async Task<IPolicyConfiguration> GetPolicyConfiguration(string name)
	{
		var raw = await db.PolicyConfiguration.Where(p => p.Name == name).Select(p => p.Data).FirstOrDefaultAsync();
		return await policySvc.GetConfiguration(name, raw) ?? throw GracefulException.NotFound("Policy not found");
	}

	[HttpPut("policy/{name}")]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.BadRequest, HttpStatusCode.NotFound)]
	public async Task UpdateWordRejectPolicy(
		string name, [SwaggerBodyExample("{\n  \"enabled\": true\n}")] JsonDocument body
	)
	{
		var type = await policySvc.GetConfigurationType(name) ?? throw GracefulException.NotFound("Policy not found");
		var data = body.Deserialize(type, JsonSerialization.Options) as IPolicyConfiguration;
		if (data?.GetType() != type) throw GracefulException.BadRequest("Invalid policy config");
		var serialized = JsonSerializer.Serialize(data, type, JsonSerialization.Options);

		await db.PolicyConfiguration
		        .Upsert(new PolicyConfiguration { Name = name, Data = serialized })
		        .On(p => new { p.Name })
		        .RunAsync();

		await policySvc.Update();
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