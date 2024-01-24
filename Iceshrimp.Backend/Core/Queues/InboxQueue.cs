using Iceshrimp.Backend.Core.Services;

namespace Iceshrimp.Backend.Core.Queues;

public class InboxQueue {
	public static JobQueue<InboxJob> Create() => new(InboxQueueProcessor, 4);

	private static Task InboxQueueProcessor(InboxJob job, IServiceScope scope, CancellationToken token) {
		//TODO
		return Task.CompletedTask;
	}
}

public class InboxJob : Job {
	//TODO
}