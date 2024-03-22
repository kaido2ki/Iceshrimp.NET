using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Services;
using Microsoft.EntityFrameworkCore;
using ProtoBuf;
using StackExchange.Redis;

namespace Iceshrimp.Backend.Core.Queues;

public class DeliverQueue
{
	public static JobQueue<DeliverJob> Create(IConnectionMultiplexer redis, string prefix)
	{
		return new JobQueue<DeliverJob>("deliver", DeliverQueueProcessorDelegateAsync, 20, redis, prefix);
	}

	private static async Task DeliverQueueProcessorDelegateAsync(
		DeliverJob job, IServiceProvider scope,
		CancellationToken token
	)
	{
		var logger     = scope.GetRequiredService<ILogger<DeliverQueue>>();
		var httpClient = scope.GetRequiredService<HttpClient>();
		var httpRqSvc  = scope.GetRequiredService<HttpRequestService>();
		var cache      = scope.GetRequiredService<CacheService>();
		var db         = scope.GetRequiredService<DatabaseContext>();
		var fedCtrl    = scope.GetRequiredService<ActivityPub.FederationControlService>();
		var followup   = scope.GetRequiredService<FollowupTaskService>();

		if (await fedCtrl.ShouldBlockAsync(job.InboxUrl, job.RecipientHost))
		{
			logger.LogDebug("Refusing to deliver activity to blocked instance ({uri})", job.InboxUrl);
			return;
		}

		if (await fedCtrl.ShouldSkipAsync(job.RecipientHost))
		{
			logger.LogDebug("fedCtrl.ShouldSkipAsync returned true, skipping");
			return;
		}

		logger.LogDebug("Delivering activity to: {uri}", job.InboxUrl);

		var key = await cache.FetchAsync($"userPrivateKey:{job.UserId}", TimeSpan.FromMinutes(60), async () =>
		{
			var keypair =
				await db.UserKeypairs.FirstOrDefaultAsync(p => p.UserId == job.UserId, token);
			return keypair?.PrivateKey ?? throw new Exception($"Failed to get keypair for user {job.UserId}");
		});

		var request =
			await httpRqSvc.PostSignedAsync(job.InboxUrl, job.Payload, job.ContentType, job.UserId, key);

		try
		{
			var response = await httpClient.SendAsync(request, token).WaitAsync(TimeSpan.FromSeconds(10), token);

			_ = followup.ExecuteTask("UpdateInstanceMetadata", async provider =>
			{
				var instanceSvc = provider.GetRequiredService<InstanceService>();
				await instanceSvc.UpdateInstanceStatusAsync(job.RecipientHost, new Uri(job.InboxUrl).Host,
				                                            (int)response.StatusCode, !response.IsSuccessStatusCode);
			});

			response.EnsureSuccessStatusCode();
		}
		catch (Exception e)
		{
			//TODO: prune dead instances after a while (and only resume sending activities after they come back)

			if (job.RetryCount++ < 10)
			{
				var jitter     = TimeSpan.FromSeconds(new Random().Next(0, 60));
				var baseDelay  = TimeSpan.FromMinutes(1);
				var maxBackoff = TimeSpan.FromHours(8);
				var backoff    = (Math.Pow(2, job.RetryCount) - 1) * baseDelay;
				if (backoff > maxBackoff)
					backoff = maxBackoff;
				backoff += jitter;

				job.ExceptionMessage = e.Message;
				job.ExceptionSource  = e.Source;
				job.DelayedUntil     = DateTime.Now + backoff;
				job.Status           = Job.JobStatus.Delayed;
			}
			else
			{
				throw;
			}
		}
	}
}

[ProtoContract]
public class DeliverJob : Job
{
	[ProtoMember(1)] public required string InboxUrl;
	[ProtoMember(2)] public required string Payload;
	[ProtoMember(3)] public required string ContentType;

	[ProtoMember(10)] public required string UserId;
	[ProtoMember(11)] public required string RecipientHost;
}