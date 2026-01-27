using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Federation.ActivityStreams;
using Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;
using Iceshrimp.Backend.Core.Queues;
using Iceshrimp.Backend.Core.Services;
using Iceshrimp.Utils.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace Iceshrimp.Backend.Core.Federation.ActivityPub;

public class ActivityDeliverService(
	ILogger<ActivityDeliverService> logger,
	QueueService queueService,
	DatabaseContext db
) : IScopedService
{
	public async Task DeliverToFollowersAsync(ASActivity activity, User actor, IEnumerable<User> recipients)
	{
		logger.LogDebug("Queuing deliver-to-followers jobs for activity {id}", activity.Id);
		if (activity.Actor == null) throw new Exception("Actor must not be null");

		// @formatter:off
		await queueService.PreDeliverQueue.EnqueueAsync(new PreDeliverJobData
		{
			ActorId            = actor.Id,
			RecipientIds       = recipients.Select(p => p.Id).Distinct().ToList(),
			SerializedActivity = JsonConvert.SerializeObject(activity, LdHelpers.JsonSerializerSettings),
			DeliverToFollowers = true
		});
		// @formatter:on
	}

	public async Task DeliverToAsync(ASActivity activity, User actor, params IEnumerable<User> recipients)
	{
		logger.LogDebug("Queuing deliver-to-recipients jobs for activity {id}", activity.Id);
		if (activity.Actor == null) throw new Exception("Actor must not be null");

		// @formatter:off
		await queueService.PreDeliverQueue.EnqueueAsync(new PreDeliverJobData
		{
			ActorId            = actor.Id,
			RecipientIds       = recipients.Select(p => p.Id).Distinct().ToList(),
			SerializedActivity = JsonConvert.SerializeObject(activity, LdHelpers.JsonSerializerSettings),
			DeliverToFollowers = false
		});
		// @formatter:on
	}

	public async Task DeliverToConditionalAsync(ASActivity activity, User actor, Note note, IEnumerable<User> recipients)
	{
		if (note.Visibility == Note.NoteVisibility.Specified)
			await DeliverToAsync(activity, actor, recipients);
		else
			await DeliverToFollowersAsync(activity, actor, recipients);
	}

	public Task<User[]> GetRecipientsAsync(Note note) => GetRecipientsAsync(note.VisibleUserIds.Prepend(note.User.Id));
	
	public async Task<User[]> GetRecipientsAsync(IEnumerable<string> recipientIds)
		=> await db.Users
		           .Where(p => recipientIds.Contains(p.Id))
		           .Where(p => p.IsRemoteUser)
		           .Select(p => new User { Id = p.Id })
		           .ToArrayAsync();

	public async Task DeliverToAsync(ASActivity activity, User actor, string recipientInbox)
	{
		logger.LogDebug("Queuing deliver-to-inbox job for activity {id}", activity.Id);
		if (activity.Actor == null) throw new Exception("Actor must not be null");

		await queueService.DeliverQueue.EnqueueAsync(new DeliverJobData
		{
			RecipientHost = new Uri(recipientInbox).Host,
			InboxUrl      = recipientInbox,
			Payload       = activity.CompactToPayload(),
			ContentType   = "application/activity+json",
			UserId        = actor.Id
		});
	}
}