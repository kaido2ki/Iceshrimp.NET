using Iceshrimp.Backend.Core.Federation.ActivityPub;
using Iceshrimp.Backend.Core.Federation.ActivityStreams;
using Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;
using Iceshrimp.Backend.Core.Services;
using Newtonsoft.Json.Linq;

namespace Iceshrimp.Backend.Core.Queues;

public class InboxQueue {
	public static JobQueue<InboxJob> Create() {
		return new JobQueue<InboxJob>("inbox", InboxQueueProcessor, 4);
	}

	private static async Task InboxQueueProcessor(InboxJob job, IServiceProvider scope, CancellationToken token) {
		var expanded = LdHelpers.Expand(job.Body);
		if (expanded == null) throw new Exception("Failed to expand ASObject");
		var obj = ASObject.Deserialize(expanded);
		if (obj == null) throw new Exception("Failed to deserialize ASObject");
		if (obj is not ASActivity activity) throw new NotImplementedException("Job data is not an ASActivity");

		var apSvc  = scope.GetRequiredService<APService>();
		var logger = scope.GetRequiredService<ILogger<InboxQueue>>();
		logger.LogTrace("Preparation took {ms} ms", job.Duration);
		await apSvc.PerformActivity(activity, job.InboxUserId);
	}
}

public class InboxJob(JToken body, string? inboxUserId) : Job {
	public readonly JToken  Body        = body;
	public readonly string? InboxUserId = inboxUserId;
}