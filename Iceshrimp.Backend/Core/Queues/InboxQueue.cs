using Iceshrimp.Backend.Core.Federation.ActivityPub;
using Iceshrimp.Backend.Core.Federation.ActivityStreams;
using Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;
using Iceshrimp.Backend.Core.Services;
using Newtonsoft.Json.Linq;
using ProtoBuf;
using StackExchange.Redis;

namespace Iceshrimp.Backend.Core.Queues;

public class InboxQueue {
	public static JobQueue<InboxJob> Create(IConnectionMultiplexer redis, string prefix) {
		return new JobQueue<InboxJob>("inbox", InboxQueueProcessorDelegateAsync, 4, redis, prefix);
	}

	private static async Task InboxQueueProcessorDelegateAsync(
		InboxJob job,
		IServiceProvider scope,
		CancellationToken token
	) {
		var expanded = LdHelpers.Expand(JToken.Parse(job.Body));
		if (expanded == null) throw new Exception("Failed to expand ASObject");
		var obj = ASObject.Deserialize(expanded);
		if (obj == null) throw new Exception("Failed to deserialize ASObject");
		if (obj is not ASActivity activity) throw new NotImplementedException("Job data is not an ASActivity");

		var apHandler = scope.GetRequiredService<ActivityHandlerService>();
		var logger    = scope.GetRequiredService<ILogger<InboxQueue>>();
		logger.LogTrace("Preparation took {ms} ms", job.Duration);
		await apHandler.PerformActivityAsync(activity, job.InboxUserId);
	}
}

[ProtoContract]
public class InboxJob : Job {
	[ProtoMember(1)] public required string  Body        { get; set; }
	[ProtoMember(2)] public required string? InboxUserId { get; set; }
}