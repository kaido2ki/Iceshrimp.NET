using System.Diagnostics.CodeAnalysis;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;
using Iceshrimp.Backend.Core.Middleware;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Core.Federation.ActivityPub;

public class ActivityRenderer(
	IOptions<Config.InstanceSection> config,
	UserRenderer userRenderer,
	NoteRenderer noteRenderer
) : IScopedService
{
	private string GenerateActivityId() =>
		$"https://{config.Value.WebDomain}/activities/ephemeral/{Guid.NewGuid().ToStringLower()}";

	public static ASCreate RenderCreate(ASNote obj, ASObject actor) => new()
	{
		Id     = $"{obj.Id}/activity",
		Actor  = ASActor.FromObject(actor),
		Object = obj,
		To     = obj.To,
		Cc     = obj.Cc
	};

	public static ASUpdate RenderUpdate(ASNote obj, ASObject actor) => new()
	{
		Id     = $"{obj.Id}#Update/{(int)(obj.UpdatedAt ?? DateTime.UtcNow).Subtract(DateTime.UnixEpoch).TotalSeconds}",
		Actor  = ASActor.FromObject(actor),
		Object = obj,
		To     = obj.To,
		Cc     = obj.Cc
	};

	public ASUpdate RenderUpdate(ASActor actor) => new()
	{
		Id     = GenerateActivityId(),
		Actor  = ASActor.FromObject(actor),
		Object = actor,
		To     = [new ASObjectBase($"{Constants.ActivityStreamsNs}#Public")]
	};

	public static ASDelete RenderDelete(ASActor actor, ASObject obj) => new()
	{
		Id     = $"{obj.Id}#Delete",
		Actor  = actor.Compact(),
		Object = obj
	};

	public ASAccept RenderAccept(User followee, User follower, string requestId) => new()
	{
		Id     = GenerateActivityId(),
		Actor  = userRenderer.RenderLite(followee),
		Object = RenderFollow(userRenderer.RenderLite(follower), userRenderer.RenderLite(followee), requestId)
	};

	public ASLike RenderLike(NoteLike like)
	{
		if (like.Note.UserHost == null)
			throw GracefulException.BadRequest("Refusing to render like activity: note user must be remote");
		if (like.User.IsRemoteUser)
			throw GracefulException.BadRequest("Refusing to render like activity: actor must be local");

		return new ASLike
		{
			Id     = $"https://{config.Value.WebDomain}/likes/{like.Id}",
			Actor  = userRenderer.RenderLite(like.User),
			Object = noteRenderer.RenderLite(like.Note)
		};
	}

	public ASEmojiReact RenderReact(NoteReaction reaction, Emoji? emoji)
	{
		if (reaction.User.IsRemoteUser)
			throw GracefulException.BadRequest("Refusing to render like activity: actor must be local");

		var res = new ASEmojiReact
		{
			Id      = $"https://{config.Value.WebDomain}/reactions/{reaction.Id}",
			Actor   = userRenderer.RenderLite(reaction.User),
			Object  = noteRenderer.RenderLite(reaction.Note),
			Content = reaction.Reaction
		};

		if (emoji == null) return res;
		var name = emoji.Host == null ? emoji.Name : $"{emoji.Name}@{emoji.Host}";

		var e = new ASEmoji
		{
			Id    = emoji.GetPublicUriOrNull(config.Value),
			Name  = name,
			Image = new ASImage { Url = new ASLink(emoji.PublicUrl) }
		};

		res.Tags = [e];

		return res;
	}

	public ASFollow RenderFollow(User follower, User followee, Guid? relationshipId)
	{
		if (follower.IsLocalUser && followee.IsLocalUser)
			throw GracefulException.BadRequest("Refusing to render follow activity between two remote users");
		if (follower.IsRemoteUser && followee.IsRemoteUser)
			throw GracefulException.BadRequest("Refusing to render follow activity between two local users");

		return RenderFollow(userRenderer.RenderLite(follower),
		                    userRenderer.RenderLite(followee),
		                    RenderFollowId(follower, followee, relationshipId));
	}

	public ASFollow RenderFollow(User actor, Relay relay)
	{
		return new ASFollow
		{
			Id     = $"https://{config.Value.WebDomain}/activities/follow-relay/{relay.Id}",
			Actor  = userRenderer.RenderLite(actor),
			Object = new ASObject { Id = "https://www.w3.org/ns/activitystreams#Public" }
		};
	}

	public ASActivity RenderUnfollow(User follower, User followee, Guid? relationshipId)
	{
		if (follower.IsLocalUser && followee.IsLocalUser)
			throw GracefulException.BadRequest("Refusing to render unfollow activity between two remote users");
		if (follower.IsRemoteUser && followee.IsRemoteUser)
			throw GracefulException.BadRequest("Refusing to render unfollow activity between two local users");

		if (follower.IsLocalUser)
		{
			var actor = userRenderer.RenderLite(follower);
			var obj   = userRenderer.RenderLite(followee);
			return RenderUndo(actor, RenderFollow(actor, obj, RenderFollowId(follower, followee, relationshipId)));
		}
		else
		{
			var actor = userRenderer.RenderLite(followee);
			var obj   = userRenderer.RenderLite(follower);
			return RenderReject(actor, RenderFollow(actor, obj, RenderFollowId(follower, followee, relationshipId)));
		}
	}

	private static ASFollow RenderFollow(ASObject followerActor, ASObject followeeActor, string requestId) => new()
	{
		Id     = requestId,
		Actor  = ASActor.FromObject(followerActor),
		Object = ASActor.FromObject(followeeActor)
	};

	public ASUndo RenderUndo(ASActor actor, ASObject obj) => new()
	{
		Id     = GenerateActivityId(),
		Actor  = actor.Compact(),
		Object = obj
	};

	public ASReject RenderReject(ASActor actor, ASObject obj) => new()
	{
		Id     = GenerateActivityId(),
		Actor  = actor.Compact(),
		Object = obj
	};

	public ASReject RenderReject(User followee, User follower, string requestId) => new()
	{
		Id     = GenerateActivityId(),
		Actor  = userRenderer.RenderLite(followee),
		Object = RenderFollow(userRenderer.RenderLite(follower), userRenderer.RenderLite(followee), requestId)
	};

	public ASBlock RenderBlock(ASActor actor, ASActor obj, string blockId) => new()
	{
		Id     = $"https://{config.Value.WebDomain}/blocks/{blockId}",
		Actor  = actor.Compact(),
		Object = obj.Compact()
	};

	[SuppressMessage("ReSharper", "SuggestBaseTypeForParameter", Justification = "This only makes sense for users")]
	private string RenderFollowId(User follower, User followee, Guid? relationshipId) =>
		$"https://{config.Value.WebDomain}/follows/{follower.Id}/{followee.Id}/{(relationshipId ?? Guid.NewGuid()).ToStringLower()}";

	private static ASAnnounce RenderAnnounce(
		ASNote note, ASActor actor, List<ASObjectBase> to, List<ASObjectBase> cc, string uri
	) => new()
	{
		Id     = uri,
		Actor  = actor.Compact(),
		Object = note,
		To     = to,
		Cc     = cc
	};

	public static ASAnnounce RenderAnnounce(
		ASNote note, string renoteUri, ASActor actor, Note.NoteVisibility visibility, string followersUri
	)
	{
		List<ASObjectBase> to = visibility switch
		{
			Note.NoteVisibility.Public    => [new ASLink($"{Constants.ActivityStreamsNs}#Public")],
			Note.NoteVisibility.Followers => [new ASLink(followersUri)],
			Note.NoteVisibility.Specified => throw new Exception("Announce cannot be specified"),
			_                             => []
		};

		List<ASObjectBase> cc = visibility switch
		{
			Note.NoteVisibility.Home => [new ASLink($"{Constants.ActivityStreamsNs}#Public")],
			_                        => []
		};

		return RenderAnnounce(note, actor, to, cc, $"{renoteUri}/activity");
	}

	public ASNote RenderVote(PollVote vote, Poll poll, Note note) => new()
	{
		Id           = GenerateActivityId(),
		AttributedTo = [userRenderer.RenderLite(vote.User)],
		To           = [new ASObjectBase(note.User.Uri ?? note.User.GetPublicUri(config.Value))],
		InReplyTo    = new ASObjectBase(note.Uri ?? note.GetPublicUri(config.Value)),
		Name         = poll.Choices[vote.Choice]
	};

	public ASMove RenderMove(ASActor actor, ASActor target) => new()
	{
		Id     = GenerateActivityId(),
		Actor  = actor.Compact(),
		Object = actor.Compact(),
		Target = new ASLink(target.Id)
	};

	public ASBite RenderBite(Bite bite, string target, User fallbackTo) => new()
	{
		Id          = bite.Uri ?? bite.GetPublicUri(config.Value),
		Actor       = userRenderer.RenderLite(bite.User),
		Target      = new ASObjectBase(target),
		PublishedAt = bite.CreatedAt,
		To          = userRenderer.RenderLite(fallbackTo)
	};
}
