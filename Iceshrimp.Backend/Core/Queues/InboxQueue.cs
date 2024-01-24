using Iceshrimp.Backend.Core.Federation.ActivityStreams;
using Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;
using Iceshrimp.Backend.Core.Services;
using Newtonsoft.Json.Linq;

namespace Iceshrimp.Backend.Core.Queues;

public class InboxQueue {
	public static JobQueue<InboxJob> Create() {
		return new JobQueue<InboxJob>(InboxQueueProcessor, 4);
	}

	private static async Task InboxQueueProcessor(InboxJob job, IServiceProvider scope, CancellationToken token) {
		var expanded = LdHelpers.Expand(job.Body);
		if (expanded == null) throw new Exception("Failed to expand ASObject");
		var obj = ASObject.Deserialize(expanded);
		if (obj == null) throw new Exception("Failed to deserialize ASObject");
		if (obj is not ASActivity activity) throw new NotImplementedException();

		var logger = scope.GetRequiredService<ILogger<InboxQueue>>();
		logger.LogDebug("Processing activity: {activity}", activity.Id);
	}
}

public class InboxJob(JToken body, string? userId) : Job {
	public JToken  Body   = body;
	public string? UserId = userId;
}