using System.Net;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Federation.ActivityStreams;
using Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Backend.Core.Services;
using Newtonsoft.Json.Linq;
using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;
using JR = System.Text.Json.Serialization.JsonRequiredAttribute;

namespace Iceshrimp.Backend.Core.Queues;

public class InboxQueue(int parallelism)
	: PostgresJobQueue<InboxJobData>("inbox", InboxQueueProcessorDelegateAsync, parallelism, TimeSpan.FromSeconds(120))
{
	private static async Task InboxQueueProcessorDelegateAsync(
		Job job,
		InboxJobData jobData,
		IServiceProvider scope,
		CancellationToken token
	)
	{
		var expanded = LdHelpers.Expand(JToken.Parse(jobData.Body)) ?? throw new Exception("Failed to expand ASObject");
		var obj      = ASObject.Deserialize(expanded) ?? throw new Exception("Failed to deserialize ASObject");
		if (obj is not ASActivity activity)
			throw new GracefulException(HttpStatusCode.UnprocessableEntity, "Job data is not an ASActivity",
			                            $"Type: {obj.Type}");

		var apHandler = scope.GetRequiredService<ActivityPub.ActivityHandlerService>();
		try
		{
			await apHandler.PerformActivityAsync(activity, jobData.InboxUserId, jobData.AuthenticatedUserId);
		}
		catch (InstanceBlockedException e)
		{
			var logger = scope.GetRequiredService<ILogger<InboxQueue>>();
			if (e.Host != null)
				logger.LogDebug("Refusing to process activity: Instance {host} is blocked", e.Host);
			else
				logger.LogDebug("Refusing to process activity: Instance is blocked ({uri})", e.Uri);
		}
		catch (Exception e) when (e is not GracefulException)
		{
			if (job.RetryCount++ < 10)
			{
				var jitter     = TimeSpan.FromSeconds(new Random().Next(0, 60));
				var baseDelay  = TimeSpan.FromMinutes(1);
				var maxBackoff = TimeSpan.FromHours(7);
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
}

public class InboxJobData
{
	[JR] [J("body")]                public required string  Body                { get; set; }
	[JR] [J("inboxUserId")]         public required string? InboxUserId         { get; set; }
	[JR] [J("authenticatedUserId")] public required string? AuthenticatedUserId { get; set; }
}