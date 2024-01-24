using Iceshrimp.Backend.Core.Services;

namespace Iceshrimp.Backend.Core.Queues;

public class DeliverQueue {
	public static JobQueue<DeliverJob> Create() {
		return new JobQueue<DeliverJob>(DeliverQueueProcessor, 4);
	}

	private static Task DeliverQueueProcessor(DeliverJob job, IServiceProvider scope, CancellationToken token) {
		//TODO
		return Task.CompletedTask;
	}
}

public class DeliverJob : Job {
	//TODO
}