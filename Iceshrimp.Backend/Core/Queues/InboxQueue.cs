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
	: PostgresJobQueue<InboxJobData>("inbox", InboxQueueProcessorDelegateAsync, parallelism, TimeSpan.FromSeconds(60))
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
			throw new GracefulException("Job data is not an ASActivity", $"Type: {obj.Type}");

		var apHandler = scope.GetRequiredService<ActivityPub.ActivityHandlerService>();
		try
		{
			await apHandler.PerformActivityAsync(activity, jobData.InboxUserId, jobData.AuthenticatedUserId);
		}
		catch (InstanceBlockedException e)
		{
			var logger = scope.GetRequiredService<ILogger<InboxQueue>>();
			if (e.Host != null)
				logger.LogDebug("Refusing to process activity {id}: Instance {host} is blocked", job.Id, e.Host);
			else
				logger.LogDebug("Refusing to process activity {id}: Instance is blocked ({uri})", job.Id, e.Uri);
		}
	}
}

public class InboxJobData
{
	[JR] [J("body")]                public required string  Body                { get; set; }
	[JR] [J("inboxUserId")]         public required string? InboxUserId         { get; set; }
	[JR] [J("authenticatedUserId")] public required string? AuthenticatedUserId { get; set; }
}