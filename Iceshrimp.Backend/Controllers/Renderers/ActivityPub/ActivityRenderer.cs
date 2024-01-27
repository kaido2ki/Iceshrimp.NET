using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Controllers.Renderers.ActivityPub;

public class ActivityRenderer(IOptions<Config.InstanceSection> config) {
	public static ASActivity RenderCreate(ASObject obj, ASObject actor) {
		return new ASActivity {
			Id     = $"{obj.Id}#Create",
			Type   = "https://www.w3.org/ns/activitystreams#Create",
			Actor  = new ASActor { Id = actor.Id },
			Object = obj
		};
	}

	public ASActivity RenderAccept(ASObject followeeActor, string requestId) {
		return new ASActivity {
			Id   = $"https://{config.Value.WebDomain}/activities/{new Guid().ToString().ToLowerInvariant()}",
			Type = "https://www.w3.org/ns/activitystreams#Accept",
			Actor = new ASActor {
				Id = followeeActor.Id
			},
			Object = new ASObject {
				Id = requestId
			}
		};
	}
}