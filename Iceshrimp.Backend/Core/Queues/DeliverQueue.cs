using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using ProtoBuf;
using StackExchange.Redis;

namespace Iceshrimp.Backend.Core.Queues;

public class DeliverQueue {
	public static JobQueue<DeliverJob> Create(IConnectionMultiplexer redis, string prefix) {
		return new JobQueue<DeliverJob>("deliver", DeliverQueueProcessor, 4, redis, prefix);
	}

	private static async Task DeliverQueueProcessor(DeliverJob job, IServiceProvider scope, CancellationToken token) {
		var logger     = scope.GetRequiredService<ILogger<DeliverQueue>>();
		var httpClient = scope.GetRequiredService<HttpClient>();
		var httpRqSvc  = scope.GetRequiredService<HttpRequestService>();
		var cache      = scope.GetRequiredService<IDistributedCache>();
		var db         = scope.GetRequiredService<DatabaseContext>();

		logger.LogDebug("Delivering activity to: {uri}", job.InboxUrl);

		var key = await cache.FetchAsync($"userPrivateKey:{job.UserId}", TimeSpan.FromMinutes(60), async () => {
			var keypair =
				await db.UserKeypairs.FirstOrDefaultAsync(p => p.UserId == job.UserId, token);
			return keypair?.PrivateKey ?? throw new Exception($"Failed to get keypair for user {job.UserId}");
		});

		var request =
			await httpRqSvc.PostSignedAsync(job.InboxUrl, job.Payload, job.ContentType, job.UserId, key);
		await httpClient.SendAsync(request, token);
	}
}

[ProtoContract]
public class DeliverJob : Job {
	[ProtoMember(1)] public required string InboxUrl    { get; set; }
	[ProtoMember(2)] public required string Payload     { get; set; }
	[ProtoMember(3)] public required string ContentType { get; set; }

	[ProtoMember(10)] public required string UserId { get; set; }
}