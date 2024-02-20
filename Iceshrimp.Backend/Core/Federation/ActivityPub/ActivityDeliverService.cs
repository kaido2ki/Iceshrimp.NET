using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Federation.ActivityStreams;
using Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;
using Iceshrimp.Backend.Core.Queues;
using Iceshrimp.Backend.Core.Services;
using Newtonsoft.Json;

namespace Iceshrimp.Backend.Core.Federation.ActivityPub;

public class ActivityDeliverService(ILogger<ActivityDeliverService> logger, QueueService queueService)
{
	public async Task DeliverToFollowersAsync(ASActivity activity, User actor, IEnumerable<User> recipients)
	{
		logger.LogDebug("Queuing deliver-to-followers jobs for activity {id}", activity.Id);
		if (activity.Actor == null) throw new Exception("Actor must not be null");

		await queueService.PreDeliverQueue.EnqueueAsync(new PreDeliverJob
		{
			ActorId      = actor.Id,
			RecipientIds = recipients.Select(p => p.Id).ToList(),
			SerializedActivity =
				JsonConvert.SerializeObject(activity,
				                            LdHelpers.JsonSerializerSettings),
			DeliverToFollowers = true
		});
	}

	public async Task DeliverToAsync(ASActivity activity, User actor, params User[] recipients)
	{
		logger.LogDebug("Queuing deliver-to-recipients jobs for activity {id}", activity.Id);
		if (activity.Actor == null) throw new Exception("Actor must not be null");

		await queueService.PreDeliverQueue.EnqueueAsync(new PreDeliverJob
		{
			ActorId      = actor.Id,
			RecipientIds = recipients.Select(p => p.Id).ToList(),
			SerializedActivity =
				JsonConvert.SerializeObject(activity,
				                            LdHelpers.JsonSerializerSettings),
			DeliverToFollowers = false
		});
	}
}