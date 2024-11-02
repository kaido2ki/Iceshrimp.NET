using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Helpers;
using Iceshrimp.Backend.Core.Middleware;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Services;

public class RelayService(
	DatabaseContext db,
	SystemUserService systemUserSvc,
	ActivityPub.ActivityRenderer activityRenderer,
	ActivityPub.ActivityDeliverService deliverSvc,
	ActivityPub.UserRenderer userRenderer
)
{
	public async Task SubscribeToRelay(string uri)
	{
		uri = new Uri(uri).AbsoluteUri;
		if (await db.Relays.AnyAsync(p => p.Inbox == uri)) return;

		var relay = new Relay
		{
			Id     = IdHelpers.GenerateSnowflakeId(),
			Inbox  = uri,
			Status = Relay.RelayStatus.Requesting
		};

		db.Add(relay);
		await db.SaveChangesAsync();

		var actor    = await systemUserSvc.GetRelayActorAsync();
		var activity = activityRenderer.RenderFollow(actor, relay);
		await deliverSvc.DeliverToAsync(activity, actor, uri);
	}

	public async Task UnsubscribeFromRelay(Relay relay)
	{
		var actor    = await systemUserSvc.GetRelayActorAsync();
		var follow   = activityRenderer.RenderFollow(actor, relay);
		var activity = activityRenderer.RenderUndo(userRenderer.RenderLite(actor), follow);
		await deliverSvc.DeliverToAsync(activity, actor, relay.Inbox);

		db.Remove(relay);
		await db.SaveChangesAsync();
	}

	public async Task HandleAccept(User actor, string id)
	{
		// @formatter:off
		if (await db.Relays.FirstOrDefaultAsync(p => p.Id == id) is not { } relay)
			throw GracefulException.UnprocessableEntity($"Relay with id {id} was not found");
		if (relay.Inbox != new Uri(actor.Inbox ?? throw new Exception("Relay actor must have an inbox")).AbsoluteUri)
			throw GracefulException.UnprocessableEntity($"Relay inbox ({relay.Inbox}) does not match relay actor inbox ({actor.Inbox})");
		// @formatter:on

		relay.Status       = Relay.RelayStatus.Accepted;
		actor.IsRelayActor = true;
		await db.SaveChangesAsync();
	}

	public async Task HandleReject(User actor, string id)
	{
		// @formatter:off
		if (db.Relays.FirstOrDefault(p => p.Id == id) is not { } relay)
			throw GracefulException.UnprocessableEntity($"Relay with id {id} was not found");
		if (relay.Inbox != new Uri(actor.Inbox ?? throw new Exception("Relay actor must have an inbox")).AbsoluteUri)
			throw GracefulException.UnprocessableEntity($"Relay inbox ({relay.Inbox}) does not match relay actor inbox ({actor.Inbox})");
		// @formatter:on

		relay.Status       = Relay.RelayStatus.Rejected;
		actor.IsRelayActor = true;
		await db.SaveChangesAsync();
	}
}