using System.Diagnostics.CodeAnalysis;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;
using Iceshrimp.Backend.Core.Middleware;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Core.Federation.ActivityPub;

public class ActivityRenderer(IOptions<Config.InstanceSection> config, UserRenderer userRenderer) {
	private string GenerateActivityId() =>
		$"https://{config.Value.WebDomain}/activities/{Guid.NewGuid().ToString().ToLowerInvariant()}";

	public static ASCreate RenderCreate(ASObject obj, ASObject actor) {
		return new ASCreate {
			Id     = $"{obj.Id}#Create",
			Actor  = new ASActor { Id = actor.Id },
			Object = obj
		};
	}

	public ASAccept RenderAccept(ASObject actor, ASObject obj) {
		return new ASAccept {
			Id = GenerateActivityId(),
			Actor = new ASActor {
				Id = actor.Id
			},
			Object = obj
		};
	}

	public ASFollow RenderFollow(User follower, User followee) {
		if (follower.Host == null && followee.Host == null)
			throw GracefulException.BadRequest("Refusing to render follow activity between two remote users");
		if (follower.Host != null && followee.Host != null)
			throw GracefulException.BadRequest("Refusing to render follow activity between two local users");

		return RenderFollow(userRenderer.RenderLite(follower),
		                    userRenderer.RenderLite(followee),
		                    RenderFollowId(follower, followee));
	}

	public ASActivity RenderUnfollow(User follower, User followee) {
		if (follower.Host == null && followee.Host == null)
			throw GracefulException.BadRequest("Refusing to render unfollow activity between two remote users");
		if (follower.Host != null && followee.Host != null)
			throw GracefulException.BadRequest("Refusing to render unfollow activity between two local users");

		if (follower.Host == null) {
			var actor = userRenderer.RenderLite(follower);
			var obj   = userRenderer.RenderLite(followee);
			return RenderUndo(actor, RenderFollow(actor, obj, RenderFollowId(follower, followee)));
		}
		else {
			var actor = userRenderer.RenderLite(followee);
			var obj   = userRenderer.RenderLite(follower);
			return RenderReject(actor, RenderFollow(actor, obj, RenderFollowId(follower, followee)));
		}
	}

	public static ASFollow RenderFollow(ASObject followerActor, ASObject followeeActor, string requestId) {
		return new ASFollow {
			Id     = requestId,
			Actor  = ASActor.FromObject(followerActor),
			Object = ASActor.FromObject(followeeActor)
		};
	}

	public ASUndo RenderUndo(ASActor actor, ASObject obj) {
		return new ASUndo {
			Id     = GenerateActivityId(),
			Actor  = actor.Compact(),
			Object = obj
		};
	}

	public ASReject RenderReject(ASActor actor, ASObject obj) {
		return new ASReject {
			Id     = GenerateActivityId(),
			Actor  = actor.Compact(),
			Object = obj
		};
	}

	[SuppressMessage("ReSharper", "SuggestBaseTypeForParameter", Justification = "This only makes sense for users")]
	private string RenderFollowId(User follower, User followee) {
		return $"https://{config.Value.WebDomain}/follows/{follower.Id}/{followee.Id}";
	}
}