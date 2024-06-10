using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Federation.ActivityStreams;
using Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Backend.Core.Services;
using Newtonsoft.Json.Linq;
using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;
using JR = System.Text.Json.Serialization.JsonRequiredAttribute;

namespace Iceshrimp.Backend.Core.Queues;

public class InboxQueue(int parallelism)
	: PostgresJobQueue<InboxJobData>("inbox", InboxQueueProcessorDelegateAsync, parallelism)
{
	private static async Task InboxQueueProcessorDelegateAsync(
		Job job,
		InboxJobData jobData,
		IServiceProvider scope,
		CancellationToken token
	)
	{
		var logger = scope.GetRequiredService<ILogger<InboxQueue>>();
		logger.LogDebug("Processing inbox job {id}", job.Id.ToStringLower());
		var expanded = LdHelpers.Expand(JToken.Parse(jobData.Body)) ?? throw new Exception("Failed to expand ASObject");
		var obj      = ASObject.Deserialize(expanded) ?? throw new Exception("Failed to deserialize ASObject");
		if (obj is not ASActivity activity)
			throw new GracefulException("Job data is not an ASActivity", $"Type: {obj.Type}");
		logger.LogTrace("Preparation took {ms} ms", job.Duration);

		var apHandler = scope.GetRequiredService<ActivityPub.ActivityHandlerService>();
		await apHandler.PerformActivityAsync(activity, jobData.InboxUserId, jobData.AuthenticatedUserId);
	}
}

public class InboxJobData
{
	[JR] [J("body")]                public required string  Body                { get; set; }
	[JR] [J("inboxUserId")]         public required string? InboxUserId         { get; set; }
	[JR] [J("authenticatedUserId")] public required string? AuthenticatedUserId { get; set; }
}