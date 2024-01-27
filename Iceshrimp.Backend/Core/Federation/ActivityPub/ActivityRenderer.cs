using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Core.Federation.ActivityPub;

public class ActivityRenderer(IOptions<Config.InstanceSection> config) {
	public static ASActivity RenderCreate(ASObject obj, ASObject actor) {
		return new ASActivity {
			Id     = $"{obj.Id}#Create",
			Type   = "https://www.w3.org/ns/activitystreams#Create",
			Actor  = new ASActor { Id = actor.Id },
			Object = obj
		};
	}

	public ASActivity RenderAccept(ASObject actor, ASObject obj) {
		return new ASActivity {
			Id   = $"https://{config.Value.WebDomain}/activities/{Guid.NewGuid().ToString().ToLowerInvariant()}",
			Type = "https://www.w3.org/ns/activitystreams#Accept",
			Actor = new ASActor {
				Id = actor.Id
			},
			Object = obj
		};
	}

	public ASActivity RenderFollow(ASObject followerActor, ASObject followeeActor, string requestId) {
		return new ASActivity {
			Id   = requestId,
			Type = ASActivity.Types.Follow,
			Actor = new ASActor {
				Id = followerActor.Id
			},
			Object = followeeActor
		};
	}
}