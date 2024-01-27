using Iceshrimp.Backend.Core.Services;

namespace Iceshrimp.Backend.Core.Queues;

public class DeliverQueue {
	public static JobQueue<DeliverJob> Create() {
		return new JobQueue<DeliverJob>("deliver", DeliverQueueProcessor, 4);
	}

	private static async Task DeliverQueueProcessor(DeliverJob job, IServiceProvider scope, CancellationToken token) {
		var logger     = scope.GetRequiredService<ILogger<DeliverQueue>>();
		var httpClient = scope.GetRequiredService<HttpClient>();
		logger.LogDebug("Delivering activity to: {uri}", job.Request.RequestUri!.AbsoluteUri);
		await httpClient.SendAsync(job.Request, token);
	}
}

public class DeliverJob(HttpRequestMessage request) : Job {
	public readonly HttpRequestMessage Request = request;
}