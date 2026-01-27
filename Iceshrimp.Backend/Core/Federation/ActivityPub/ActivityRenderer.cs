using System.Diagnostics.CodeAnalysis;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Utils.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Core.Federation.ActivityPub;

public class ActivityRenderer(
	IOptions<Config.InstanceSection> config,
	UserRenderer userRenderer,
	NoteRenderer noteRenderer,
	StampRenderer stampRenderer
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

	public ASDelete RenderDelete(ASActor actor, ASObject obj, Note? note = null, List<User>? recipients = null)
	{
		var (to, cc) = note != null 
			? RenderVisibility(note, recipients ?? []) 
			: ([], [new ASObjectBase($"{Constants.ActivityStreamsNs}#Public")]);

		return new ASDelete
		{
			Id = $"{obj.Id}#Delete",
			Actor = actor.Compact(),
			Object = obj,
			To = to,
			Cc = cc,
		};
	}

	public ASAccept RenderAccept(User followee, User follower, string requestId) => new()
	{
		Id     = GenerateActivityId(),
		Actor  = userRenderer.RenderLite(followee),
		Object = RenderFollow(userRenderer.RenderLite(follower), userRenderer.RenderLite(followee), requestId),
		To     = [userRenderer.RenderLite(follower)]
	};
	
	public ASAccept RenderAcceptStamp(InteractionStamp stamp, ASObject request) => new()
	{
		Id     = GenerateActivityId(),
		Actor  = userRenderer.RenderLite(stamp.TargetNote.User),
		Object = request,
		Result = new ASObjectBase(stampRenderer.StampId(stamp)),
		To     = [userRenderer.RenderLite(stamp.Note.User)]
	};
	
	public ASReject RenderRejectStamp(InteractionStamp stamp, ASObject request) => new()
	{
		Id     = GenerateActivityId(),
		Actor  = userRenderer.RenderLite(stamp.TargetNote.User),
		Object = request,
		To     = [userRenderer.RenderLite(stamp.Note.User)]
	};
	
	public ASLike RenderLike(NoteLike like, IEnumerable<User> recipients)
	{
		if (like.User.IsRemoteUser)
			throw GracefulException.BadRequest("Refusing to render like activity: actor must be local");

		var (to, cc) = RenderVisibility(like.Note, recipients);

		return new ASLike
		{
			Id     = $"https://{config.Value.WebDomain}/likes/{like.Id}",
			Actor  = userRenderer.RenderLite(like.User),
			Object = noteRenderer.RenderLite(like.Note),
			To     = to,
			Cc     = cc,
		};
	}

	public ASEmojiReact RenderReact(NoteReaction reaction, Emoji? emoji, IEnumerable<User> recipients)
	{
		if (reaction.User.IsRemoteUser)
			throw GracefulException.BadRequest("Refusing to render like activity: actor must be local");
		
		var (to, cc) = RenderVisibility(reaction.Note, recipients);

		var res = new ASEmojiReact
		{
			Id      = $"https://{config.Value.WebDomain}/reactions/{reaction.Id}",
			Actor   = userRenderer.RenderLite(reaction.User),
			Object  = noteRenderer.RenderLite(reaction.Note),
			Content = reaction.Reaction,
			To      = to,
			Cc      = cc,
		};

		if (emoji == null) return res;
		var name = emoji.Host == null ? emoji.Name : $"{emoji.Name}@{emoji.Host}";

		var e = new ASEmoji
		{
			Id    = emoji.GetPublicUriOrNull(config.Value),
			Name  = $":{name}:",
			Image = new ASImage { Url = new ASLink(emoji.RawPublicUrl), MediaType = emoji.Type }
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
			Object = new ASObject { Id = $"{Constants.ActivityStreamsNs}#Public" },
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
		Object = ASActor.FromObject(followeeActor),
		To = [new ASObjectBase(followerActor.Id)]
	};

	public ASUndo RenderUndo(ASActor actor, ASActivity obj) => new()
	{
		Id     = GenerateActivityId(),
		Actor  = actor.Compact(),
		Object = obj,
		To     = obj.To,
		Cc     = obj.Cc
	};

	public ASReject RenderReject(ASActor actor, ASActivity obj) => new()
	{
		Id     = GenerateActivityId(),
		Actor  = actor.Compact(),
		Object = obj,
		To     = obj.To,
		Cc     = obj.Cc
	};

	public ASReject RenderReject(User followee, User follower, string requestId) => new()
	{
		Id     = GenerateActivityId(),
		Actor  = userRenderer.RenderLite(followee),
		Object = RenderFollow(userRenderer.RenderLite(follower), userRenderer.RenderLite(followee), requestId),
		To     = [userRenderer.RenderLite(follower)],
	};

	public ASBlock RenderBlock(ASActor actor, ASActor obj, string blockId) => new()
	{
		Id     = $"https://{config.Value.WebDomain}/blocks/{blockId}",
		Actor  = actor.Compact(),
		Object = obj.Compact(),
		To     = [new ASObjectBase(obj.Id)],
	};

	[SuppressMessage("ReSharper", "SuggestBaseTypeForParameter", Justification = "This only makes sense for users")]
	private string RenderFollowId(User follower, User followee, Guid? relationshipId) =>
		$"https://{config.Value.WebDomain}/follows/{follower.Id}/{followee.Id}/{(relationshipId ?? Guid.NewGuid()).ToStringLower()}";

	private static ASAnnounce RenderAnnounce(
		ASNote note, ASActor actor, List<ASObjectBase> to, List<ASObjectBase> cc, string uri, DateTime publishedAt
	) => new()
	{
		Id     = uri,
		Actor  = actor.Compact(),
		Object = note,
		PublishedAt = publishedAt,
		To     = to,
		Cc     = cc
	};

	public ASAnnounce RenderAnnounce(ASNote asNote, string renoteUri, ASActor actor, Note note, IEnumerable<User> recipients)
	{
		var (to, cc) = RenderVisibility(note, recipients);

		return RenderAnnounce(asNote, actor, to, cc, $"{renoteUri}/activity", note.CreatedAt);
	}

	private (List<ASObjectBase> to, List<ASObjectBase> cc) RenderVisibility(Note note, IEnumerable<User> recipients, User? interactingUser = null)
	{
		var authorFollowersUri = note.User.FollowersUri ?? note.User.GetPublicUri(config.Value) + "/followers";
		var interactingUserFollowersUri = interactingUser != null 
			? interactingUser.FollowersUri ?? interactingUser.GetPublicUri(config.Value) + "/followers" 
			: null;

		List<ASObjectBase> to = note.Visibility switch
		{
			Note.NoteVisibility.Public    => [new ASLink($"{Constants.ActivityStreamsNs}#Public")],
			Note.NoteVisibility.Followers => [new ASLink(authorFollowersUri)],
			_                             => []
		};

		to.AddRange(recipients.Select(userRenderer.RenderLite));

		List<ASObjectBase> cc = note.Visibility switch
		{
			Note.NoteVisibility.Public when interactingUserFollowersUri != null => [new ASLink(interactingUserFollowersUri)],

			Note.NoteVisibility.Home when interactingUserFollowersUri == null => [new ASLink($"{Constants.ActivityStreamsNs}#Public")],
			Note.NoteVisibility.Home => [
				new ASLink($"{Constants.ActivityStreamsNs}#Public"),
				new ASLink(interactingUserFollowersUri)
			],
			_ => []
		};

		return (to, cc);
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
		To          = [userRenderer.RenderLite(fallbackTo)]
	};

	public ASFlag RenderFlag(User actor, User user, IEnumerable<Note> notes, string comment) => new()
	{
		Id      = GenerateActivityId(),
		Actor   = userRenderer.RenderLite(actor),
		Object  = notes.Select(noteRenderer.RenderLite).Prepend<ASObject>(userRenderer.RenderLite(user)).ToArray(),
		Content = comment
	};
}
