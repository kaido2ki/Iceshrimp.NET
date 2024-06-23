using System.Net;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Services;
using Microsoft.EntityFrameworkCore;
using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;
using JR = System.Text.Json.Serialization.JsonRequiredAttribute;

namespace Iceshrimp.Backend.Core.Queues;

public class DeliverQueue(int parallelism)
	: PostgresJobQueue<DeliverJobData>("deliver", DeliverQueueProcessorDelegateAsync,
	                                   parallelism, TimeSpan.FromSeconds(60))
{
	private static async Task DeliverQueueProcessorDelegateAsync(
		Job job, DeliverJobData jobData, IServiceProvider scope, CancellationToken token
	)
	{
		var logger     = scope.GetRequiredService<ILogger<DeliverQueue>>();
		var httpClient = scope.GetRequiredService<HttpClient>();
		var httpRqSvc  = scope.GetRequiredService<HttpRequestService>();
		var cache      = scope.GetRequiredService<CacheService>();
		var db         = scope.GetRequiredService<DatabaseContext>();
		var fedCtrl    = scope.GetRequiredService<ActivityPub.FederationControlService>();
		var followup   = scope.GetRequiredService<FollowupTaskService>();

		if (await fedCtrl.ShouldBlockAsync(jobData.InboxUrl, jobData.RecipientHost))
		{
			logger.LogDebug("Refusing to deliver activity to blocked instance ({uri})", jobData.InboxUrl);
			return;
		}

		logger.LogDebug("Delivering activity to: {uri}", jobData.InboxUrl);

		var key = await cache.FetchAsync($"userPrivateKey:{jobData.UserId}", TimeSpan.FromMinutes(60), async () =>
		{
			var keypair =
				await db.UserKeypairs.FirstOrDefaultAsync(p => p.UserId == jobData.UserId, token);
			return keypair?.PrivateKey ?? throw new Exception($"Failed to get keypair for user {jobData.UserId}");
		});

		var request =
			await httpRqSvc.PostSignedAsync(jobData.InboxUrl, jobData.Payload, jobData.ContentType, jobData.UserId,
			                                key);

		try
		{
			var response = await httpClient.SendAsync(request, token).WaitAsync(TimeSpan.FromSeconds(10), token);

			_ = followup.ExecuteTask("UpdateInstanceMetadata", async provider =>
			{
				var instanceSvc = provider.GetRequiredService<InstanceService>();
				await instanceSvc.UpdateInstanceStatusAsync(jobData.RecipientHost, new Uri(jobData.InboxUrl).Host,
				                                            (int)response.StatusCode, !response.IsSuccessStatusCode);
			});

			response.EnsureSuccessStatusCode(true, () => new ClientError(response.StatusCode));
		}
		catch (Exception e) when (e is not ClientError)
		{
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
				job.StackTrace       = e.StackTrace;
				job.Exception        = e.ToString();
				job.DelayedUntil     = DateTime.UtcNow + backoff;
				job.Status           = Job.JobStatus.Delayed;
			}
			else
			{
				throw;
			}
		}
	}

	public class ClientError(HttpStatusCode statusCode) : Exception
	{
		public HttpStatusCode StatusCode => statusCode;
	}
}

public class DeliverJobData
{
	[JR] [J("inboxUrl")]      public required string InboxUrl      { get; set; }
	[JR] [J("payload")]       public required string Payload       { get; set; }
	[JR] [J("contentType")]   public required string ContentType   { get; set; }
	[JR] [J("userId")]        public required string UserId        { get; set; }
	[JR] [J("recipientHost")] public required string RecipientHost { get; set; }
}