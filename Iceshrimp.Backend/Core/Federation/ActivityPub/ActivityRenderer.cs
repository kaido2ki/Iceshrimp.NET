using System.Diagnostics.CodeAnalysis;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;
using Iceshrimp.Backend.Core.Middleware;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Core.Federation.ActivityPub;

public class ActivityRenderer(
	IOptions<Config.InstanceSection> config,
	UserRenderer userRenderer,
	NoteRenderer noteRenderer
)
{
	private string GenerateActivityId() =>
		$"https://{config.Value.WebDomain}/activities/{Guid.NewGuid().ToString().ToLowerInvariant()}";

	public static ASCreate RenderCreate(ASNote obj, ASObject actor) => new()
	{
		Id     = $"{obj.Id}#Create",
		Actor  = ASActor.FromObject(actor),
		Object = obj,
		To     = obj.To,
		Cc     = obj.Cc
	};

	public ASUpdate RenderUpdate(ASNote obj, ASObject actor) => new()
	{
		Id     = GenerateActivityId(),
		Actor  = ASActor.FromObject(actor),
		Object = obj,
		To     = obj.To,
		Cc     = obj.Cc
	};

	public ASAccept RenderAccept(User followee, User follower, string requestId) => new()
	{
		Id     = GenerateActivityId(),
		Actor  = userRenderer.RenderLite(followee),
		Object = RenderFollow(userRenderer.RenderLite(follower), userRenderer.RenderLite(followee), requestId)
	};

	public ASAccept RenderAccept(ASActor actor, ASObject obj) => new()
	{
		Id = GenerateActivityId(), Actor = actor.Compact(), Object = obj
	};

	public ASLike RenderLike(Note note, User user)
	{
		if (note.UserHost == null)
			throw GracefulException.BadRequest("Refusing to render like activity: note user must be remote");
		if (user.Host != null)
			throw GracefulException.BadRequest("Refusing to render like activity: actor must be local");

		return new ASLike
		{
			Id = GenerateActivityId(), Actor = userRenderer.RenderLite(user), Object = noteRenderer.RenderLite(note)
		};
	}

	public ASFollow RenderFollow(User follower, User followee)
	{
		if (follower.Host == null && followee.Host == null)
			throw GracefulException.BadRequest("Refusing to render follow activity between two remote users");
		if (follower.Host != null && followee.Host != null)
			throw GracefulException.BadRequest("Refusing to render follow activity between two local users");

		return RenderFollow(userRenderer.RenderLite(follower),
		                    userRenderer.RenderLite(followee),
		                    RenderFollowId(follower, followee));
	}

	public ASActivity RenderUnfollow(User follower, User followee)
	{
		if (follower.Host == null && followee.Host == null)
			throw GracefulException.BadRequest("Refusing to render unfollow activity between two remote users");
		if (follower.Host != null && followee.Host != null)
			throw GracefulException.BadRequest("Refusing to render unfollow activity between two local users");

		if (follower.Host == null)
		{
			var actor = userRenderer.RenderLite(follower);
			var obj   = userRenderer.RenderLite(followee);
			return RenderUndo(actor, RenderFollow(actor, obj, RenderFollowId(follower, followee)));
		}
		else
		{
			var actor = userRenderer.RenderLite(followee);
			var obj   = userRenderer.RenderLite(follower);
			return RenderReject(actor, RenderFollow(actor, obj, RenderFollowId(follower, followee)));
		}
	}

	public static ASFollow RenderFollow(ASObject followerActor, ASObject followeeActor, string requestId) => new()
	{
		Id = requestId, Actor = ASActor.FromObject(followerActor), Object = ASActor.FromObject(followeeActor)
	};

	public ASUndo RenderUndo(ASActor actor, ASObject obj) => new()
	{
		Id = GenerateActivityId(), Actor = actor.Compact(), Object = obj
	};

	public ASReject RenderReject(ASActor actor, ASObject obj) => new()
	{
		Id = GenerateActivityId(), Actor = actor.Compact(), Object = obj
	};

	public ASReject RenderReject(User followee, User follower, string requestId) => new()
	{
		Id     = GenerateActivityId(),
		Actor  = userRenderer.RenderLite(followee),
		Object = RenderFollow(userRenderer.RenderLite(follower), userRenderer.RenderLite(followee), requestId)
	};

	[SuppressMessage("ReSharper", "SuggestBaseTypeForParameter", Justification = "This only makes sense for users")]
	private string RenderFollowId(User follower, User followee) =>
		$"https://{config.Value.WebDomain}/follows/{follower.Id}/{followee.Id}";
}