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
using Iceshrimp.EntityFrameworkCore.Extensions;
using Iceshrimp.Shared.Configuration;
using Iceshrimp.Shared.Schemas.Web;
using Microsoft.AspNetCore.Cors;
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
[EnableCors("iceshrimp")]
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
	PolicyService policySvc,
	EventService eventSvc
) : ControllerBase
{
	/// <summary>
	/// Generate invite code
	/// </summary>
	/// <remarks>
	/// Generate an invite code which can be used to register an account if <c>[Security] Registrations = Invite</c>.
	/// </remarks>
	/// <response code="200">Generated invite code</response>
	[HttpPost("invites/generate")]
	[Produces(MediaTypeNames.Application.Json)]
	[ProducesResults(HttpStatusCode.OK)]
	public async Task<InviteResponse> GenerateInvite()
	{
		var user = HttpContext.GetUserOrFail();
		var invite = new RegistrationInvite
		{
			Id        = IdHelpers.GenerateSnowflakeId(),
			CreatedAt = DateTime.UtcNow,
			CreatedBy = user,
			Code      = CryptographyHelpers.GenerateRandomString(32)
		};

		await db.AddAsync(invite);
		await db.SaveChangesAsync();

		return new InviteResponse { Code = invite.Code };
	}

	/// <summary>
	/// Revoke invite code
	/// </summary>
	/// <remarks>
	/// Revoke an invite code so it can no longer be used to register accounts.
	/// </remarks>
	/// <param name="code">Invite code to revoke</param>
	[HttpPost("invites/{code}/revoke")]
	[ProducesResults(HttpStatusCode.OK)]
	public async Task<OkResult> RevokeInvite(string code)
	{
		await db.RegistrationInvites.Where(p => p.Code == code).ExecuteDeleteAsync();
		return Ok();
	}

	/// <summary>
	/// Reset user password
	/// </summary>
	/// <remarks>
	/// Reset a user's password to a provided value so they can regain access to their account.
	/// </remarks>
	/// <param name="id">The user's ID</param>
	/// <param name="request">Reset password request</param>
	/// <response code="400">Password must be at least 8 characters long</response>
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

	/// <summary>
	/// Reset two-factor authentication
	/// </summary>
	/// <remarks>
	/// Reset a user's two-factor authentication so they can regain access to their account.
	/// </remarks>
	/// <param name="id">The user's ID</param>
	[HttpPost("users/{id}/reset-2fa")]
	[Produces(MediaTypeNames.Application.Json)]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.NotFound)]
	public async Task ResetTwoFactor(string id)
	{
		var settings = await db.UserSettings.FirstOrDefaultAsync(p => p.UserId == id) ??
		               throw GracefulException.RecordNotFound();

		settings.TwoFactorEnabled    = false;
		settings.TwoFactorSecret     = null;
		settings.TwoFactorTempSecret = null;

		await db.SaveChangesAsync();
	}

	/// <summary>
	/// List allowed instances
	/// </summary>
	/// <remarks>If <c>[Security] FederationMode = AllowList</c>, returns the list of instances which are allowed to federate.</remarks>
	/// <param name="limit">Pagination limit</param>
	/// <param name="offset">Pagination offset</param>
	/// <response code="400">Federation mode is set to blocklist.</response>
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

	/// <summary>
	/// List blocked instances
	/// </summary>
	/// <remarks>If <c>[Security] FederationMode = BlockList</c>, returns the list of instances which are blocked from federating and the block reasons.</remarks>
	/// <param name="limit">Pagination limit</param>
	/// <param name="offset">Pagination offset</param>
	/// <response code="400">Federation mode is set to allowlist.</response>
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

	/// <summary>
	/// Import allowed instances
	/// </summary>
	/// <remarks>
	/// Import a list of instances that are allowed to federate if <c>[Security] FederationMode = AllowList</c>.
	/// <code>instance</code>
	/// </remarks>
	/// <param name="file">CSV file containing a list of instances</param>
	/// <response code="400">Federation mode is set to blocklist.</response>
	[HttpPost("instances/allowed/import")]
	[ProducesResults(HttpStatusCode.Accepted)]
	[ProducesErrors(HttpStatusCode.BadRequest)]
	public async Task<AcceptedResult> ImportAllowedInstances(IFormFile file)
	{
		if (security.Value.FederationMode == Enums.FederationMode.BlockList)
			throw GracefulException.BadRequest("Federation mode is set to blocklist.");

		var reader = new StreamReader(file.OpenReadStream());
		var data   = await reader.ReadToEndAsync().ContinueWithResult(res => res.Trim());

		AllowedInstance[]? hosts = null;
		if (data.StartsWith('[') && data.EndsWith(']'))
		{
			try
			{
				hosts = JsonSerializer.Deserialize<AllowedInstance[]>(data, JsonSerialization.Options);
				foreach (var instance in hosts ?? [])
					instance.IsImported = true;
			}
			catch
			{
				// ignored
			}
		}

		hosts ??= data.ReplaceLineEndings("\n")
		              .Split('\n')
		              .Where(p => !string.IsNullOrWhiteSpace(p))
		              .Select(p => new AllowedInstance { Host = p.ToPunycodeLower(), IsImported = true })
		              .ToArray();

		await db.AllowedInstances.UpsertRange(hosts).On(p => p.Host).NoUpdate().RunAsync();

		return Accepted();
	}

	/// <summary>
	/// Import blocked instances
	/// </summary>
	/// <remarks>
	/// Import a list of instances that are not allowed to federate if <c>[Security] FederationMode = BlockList</c>.
	/// <code>instance,reason</code>
	/// </remarks>
	/// <param name="file">CSV file containing a list of instances and reasons</param>
	/// <response code="400">Federation mode is set to allowlist.</response>
	[HttpPost("instances/blocked/import")]
	[ProducesResults(HttpStatusCode.Accepted)]
	[ProducesErrors(HttpStatusCode.BadRequest)]
	public async Task<AcceptedResult> ImportBlockedInstances(IFormFile file)
	{
		if (security.Value.FederationMode == Enums.FederationMode.AllowList)
			throw GracefulException.BadRequest("Federation mode is set to allowlist.");

		var reader = new StreamReader(file.OpenReadStream());
		var data   = await reader.ReadToEndAsync().ContinueWithResult(res => res.Trim());

		BlockedInstance[]? hosts = null;
		if (data.StartsWith('[') && data.EndsWith(']'))
		{
			try
			{
				hosts = JsonSerializer.Deserialize<BlockedInstance[]>(data, JsonSerialization.Options);
				foreach (var instance in hosts ?? [])
					instance.IsImported = true;
			}
			catch
			{
				// ignored
			}
		}

		hosts ??= data.ReplaceLineEndings("\n")
		              .Split('\n')
		              .Where(p => !string.IsNullOrWhiteSpace(p))
		              .Select(p => new BlockedInstance { Host = p.ToPunycodeLower(), IsImported = true })
		              .ToArray();

		await db.BlockedInstances.UpsertRange(hosts).On(p => p.Host).NoUpdate().RunAsync();

		return Accepted();
	}

	/// <summary>
	/// Allow instance
	/// </summary>
	/// <remarks>
	/// Adds or updates an instance in the allow list. It is recommended to use the root domain (e.g. <c>example.org</c> instead of <c>shrimp.example.org</c>) when adding new instances.
	/// </remarks>
	/// <param name="host" example="example.org">Instance domain name</param>
	/// <param name="imported">Imported flag</param>
	/// <response code="400">Federation mode is set to blocklist.</response>
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

	/// <summary>
	/// Block instance
	/// </summary>
	/// <remarks>
	/// Adds or updates an instance in the block list. It is recommended to use the root domain (e.g. <c>example.org</c> instead of <c>shrimp.example.org</c>) when adding new instances.
	/// </remarks>
	/// <param name="host" example="example.org">Instance domain name</param>
	/// <param name="imported">Imported flag</param>
	/// <param name="reason">Reason for the block</param>
	/// <response code="400">Federation mode is set to allowlist.</response>
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

	/// <summary>
	/// Disallow instance
	/// </summary>
	/// <remarks>
	/// Removes an instance from the allow list.
	/// </remarks>
	/// <param name="host" example="example.org">Instance domain name</param>
	[HttpPost("instances/{host}/disallow")]
	[ProducesResults(HttpStatusCode.OK)]
	public async Task DisallowInstance(string host)
	{
		if (security.Value.FederationMode == Enums.FederationMode.BlockList)
			throw GracefulException.BadRequest("Federation mode is set to blocklist.");

		await db.AllowedInstances.Where(p => p.Host == host.ToPunycodeLower()).ExecuteDeleteAsync();
	}

	/// <summary>
	/// Remove bubble instance
	/// </summary>
	/// <remarks>
	/// Removes an instance from the bubble.
	/// </remarks>
	/// <param name="host" example="example.org">Instance domain name</param>
	[HttpPost("instances/{host}/debubble")]
	[ProducesResults(HttpStatusCode.OK)]
	public async Task DebubbleInstance(string host)
	{
		var res = await db.BubbleInstances.Where(p => p.Host == host.ToPunycodeLower()).ExecuteDeleteAsync();
		if (res > 0) eventSvc.RaiseBubbleInstanceRemoved(new BubbleInstance { Host = host });
	}

	/// <summary>
	/// Unblock instance
	/// </summary>
	/// <remarks>
	/// Removes an instance from the block list.
	/// </remarks>
	/// <param name="host" example="example.org">Instance domain name</param>
	[HttpPost("instances/{host}/unblock")]
	[ProducesResults(HttpStatusCode.OK)]
	public async Task UnblockInstance(string host)
	{
		if (security.Value.FederationMode == Enums.FederationMode.AllowList)
			throw GracefulException.BadRequest("Federation mode is set to allowlist.");

		await db.BlockedInstances.Where(p => p.Host == host.ToPunycodeLower()).ExecuteDeleteAsync();
	}

	/// <summary>
	/// Override instance status
	/// </summary>
	/// <param name="host" example="example.org">Instance domain name</param>
	/// <param name="state">State the instance should be set to</param>
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

	/// <summary>
	/// Retry job
	/// </summary>
	/// <param name="id">The job's ID</param>
	[HttpPost("queue/jobs/{id::guid}/retry")]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.BadRequest, HttpStatusCode.NotFound)]
	public async Task RetryQueueJob(Guid id)
	{
		var job = await db.Jobs.FirstOrDefaultAsync(p => p.Id == id) ??
		          throw GracefulException.NotFound($"Job {id} was not found.");

		await queueSvc.RetryJobAsync(job);
	}

	/// <summary>
	/// Retry all jobs
	/// </summary>
	/// <remarks>Retry all jobs in a queue.</remarks>
	/// <param name="queue">Name of the queue</param>
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

	/// <summary>
	/// Retry jobs
	/// </summary>
	/// <remarks>Retry a range of jobs in a queue.</remarks>
	/// <param name="queue">Name of the queue</param>
	/// <param name="from">Inclusive range start</param>
	/// <param name="to">Inclusive range end</param>
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

	/// <summary>
	/// Abandon job
	/// </summary>
	/// <param name="id">The job's ID</param>
	[HttpPost("queue/jobs/{id::guid}/abandon")]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.BadRequest, HttpStatusCode.NotFound)]
	public async Task AbandonQueueJob(Guid id)
	{
		var job = await db.Jobs.FirstOrDefaultAsync(p => p.Id == id) ??
		          throw GracefulException.NotFound($"Job {id} was not found.");

		await queueSvc.AbandonJobAsync(job);
	}

	/// <summary>
	/// List relays
	/// </summary>
	/// <remarks>Returns a list of ActivityPub relays and their statuses.</remarks>
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

	/// <summary>
	/// Add relay
	/// </summary>
	/// <remarks>Adds an ActivityPub relay.</remarks>
	/// <param name="rq">Relay request</param>
	[HttpPost("relays")]
	[ProducesResults(HttpStatusCode.OK)]
	public async Task SubscribeToRelay(RelaySchemas.RelayRequest rq)
	{
		await relaySvc.SubscribeToRelayAsync(rq.Inbox);
	}

	/// <summary>
	/// Remove relay
	/// </summary>
	/// <param name="id">The relay's ID.</param>
	[HttpDelete("relays/{id}")]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.NotFound)]
	public async Task UnsubscribeFromRelay(string id)
	{
		var relay = await db.Relays.FirstOrDefaultAsync(p => p.Id == id) ??
		            throw GracefulException.NotFound("Relay not found");
		await relaySvc.UnsubscribeFromRelayAsync(relay);
	}

	/// <summary>
	/// Prune expired media
	/// </summary>
	/// <remarks>Delete all files from the Drive that have expired.</remarks>
	[HttpPost("drive/prune-expired-media")]
	[ProducesResults(HttpStatusCode.OK)]
	public async Task PruneExpiredMedia([FromServices] IServiceScopeFactory factory)
	{
		await using var scope = factory.CreateAsyncScope();
		await new MediaCleanupTask().InvokeAsync(scope.ServiceProvider);
	}

	/// <summary>
	/// Run cron task
	/// </summary>
	/// <param name="id">The task's ID.</param>
	[HttpPost("tasks/{id}/run")]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.NotFound)]
	public void RunCronTask([FromServices] CronService cronSvc, string id)
	{
		var task = cronSvc.Tasks.FirstOrDefault(p => p.Task.GetType().FullName == id)
		           ?? throw GracefulException.NotFound("Task not found");

		Task.Run(async () =>
		         {
			         await cronSvc.RunCronTaskAsync(task.Task, task.Trigger);
			         task.Trigger.UpdateNextTrigger();
		         },
		         CancellationToken.None);
	}

	/// <summary>
	/// List policies
	/// </summary>
	/// <remarks>
	/// Returns a list of available policy names. Policies are Iceshrimp.NET's message rewrite facility. They allow instances admins to modify incoming notes as they are federated.
	/// </remarks>
	[HttpGet("policy")]
	[ProducesResults(HttpStatusCode.OK)]
	public async Task<List<string>> GetAvailablePolicies() => await policySvc.GetAvailablePoliciesAsync();

	/// <summary>
	/// Get policy
	/// </summary>
	/// <remarks>Returns the configuration for a policy.</remarks>
	/// <param name="name" example="WordRejectPolicy">The policy's name</param>
	/// <response code="200">The policy's configuration</response>
	[HttpGet("policy/{name}")]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.NotFound)]
	public async Task<IPolicyConfiguration> GetPolicyConfiguration(string name)
	{
		var raw = await db.PolicyConfiguration.Where(p => p.Name == name).Select(p => p.Data).FirstOrDefaultAsync();
		return await policySvc.GetConfigurationAsync(name, raw) ?? throw GracefulException.NotFound("Policy not found");
	}

	/// <summary>
	/// Set policy
	/// </summary>
	/// <remarks>Updates the configuration of a policy.</remarks>
	/// <param name="name" example="WordRejectPolicy">The policy's name</param>
	/// <param name="body">Policy configuration</param>
	[HttpPut("policy/{name}")]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.BadRequest, HttpStatusCode.NotFound)]
	[OverrideRequestBodyExample("{\n  \"enabled\": true\n}")]
	public async Task UpdateWordRejectPolicy(string name, JsonDocument body)
	{
		var type = await policySvc.GetConfigurationTypeAsync(name) ??
		           throw GracefulException.NotFound("Policy not found");
		var data = body.Deserialize(type, JsonSerialization.Options) as IPolicyConfiguration;
		if (data?.GetType() != type) throw GracefulException.BadRequest("Invalid policy config");
		var serialized = JsonSerializer.Serialize(data, type, JsonSerialization.Options);

		await db.PolicyConfiguration
		        .Upsert(new PolicyConfiguration { Name = name, Data = serialized })
		        .On(p => new { p.Name })
		        .RunAsync();

		await policySvc.UpdateAsync();
	}

	/// <summary>
	/// Get note activity
	/// </summary>
	/// <remarks>
	/// Returns the ActivityPub representation of a local note.
	/// </remarks>
	/// <param name="id">The note's ID</param>
	/// <response code="200">Note Activity</response>
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

	/// <summary>
	/// Get announce activity
	/// </summary>
	/// <remarks>
	/// Returns the ActivityPub representation of a local renote.
	/// </remarks>
	/// <param name="id">The renote's ID</param>
	/// <response code="200">Announce Activity</response>
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
		                                  note.User.GetPublicUri(config.Value) + "/followers",
		                                  note.CreatedAt)
		                  .Compact();
	}

	/// <summary>
	/// Get person activity by ID
	/// </summary>
	/// <remarks>
	/// Returns the ActivityPub representation of a user.
	/// </remarks>
	/// <param name="id">The user's ID</param>
	/// <response code="200">Person Activity</response>
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

	/// <summary>
	/// Get featured notes activity
	/// </summary>
	/// <remarks>
	/// Returns the ActivityPub representation of a local user's pinned notes.
	/// </remarks>
	/// <param name="id">The user's ID</param>
	/// <response code="200">OrderedCollection Activity</response>
	[UseNewtonsoftJson]
	[HttpGet("activities/users/{id}/collections/featured")]
	[OverrideResultType<ASOrderedCollection>]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesActivityStreamsPayload]
	public async Task<JObject> GetUserFeaturedActivity(string id)
	{
		return await apController.GetUserFeatured(id);
	}

	/// <summary>
	/// Get person activity by username
	/// </summary>
	/// <remarks>
	/// Returns the ActivityPub representation of a user.
	/// </remarks>
	/// <param name="acct" example="username@example.org">The user's username</param>
	/// <response code="200">Person Activity</response>
	[UseNewtonsoftJson]
	[HttpGet("activities/users/@{acct}")]
	[OverrideResultType<ASActor>]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesActivityStreamsPayload]
	public async Task<ActionResult<JObject>> GetUserActivityByUsername(string acct)
	{
		return await apController.GetUserByUsername(acct);
	}

	/// <summary>
	/// Get activity
	/// </summary>
	/// <remarks>Returns an ActivityPub Activity.</remarks>
	/// <param name="uri">The object's URI</param>
	/// <param name="userId">The viewing user's ID (if the Activity is not public)</param>
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

	/// <summary>
	/// Get raw activity
	/// </summary>
	/// <remarks>Returns an ActivityPub Activity.</remarks>
	/// <param name="uri">The object's URI</param>
	/// <param name="userId">The viewing user's ID (if the Activity is not public)</param>
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