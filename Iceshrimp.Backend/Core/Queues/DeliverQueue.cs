using Iceshrimp.Backend.Core.Services;

namespace Iceshrimp.Backend.Core.Queues;

public class DeliverQueue {
	public static JobQueue<DeliverJob> Create() => new(DeliverQueueProcessor, 4);

	private static Task DeliverQueueProcessor(DeliverJob job, IServiceScope scope, CancellationToken token) {
		//TODO
		return Task.CompletedTask;
	}
}

public class DeliverJob : Job {
	//TODO
}