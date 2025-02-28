using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database.Tables;
using J = Newtonsoft.Json.JsonPropertyAttribute;
using JC = Newtonsoft.Json.JsonConverterAttribute;
using JR = Newtonsoft.Json.JsonRequiredAttribute;
using JI = Newtonsoft.Json.JsonIgnoreAttribute;
using VC = Iceshrimp.Backend.Core.Federation.ActivityStreams.Types.ValueObjectConverter;

namespace Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;

public class ASActivity : ASObjectWithId
{
	[J($"{Constants.ActivityStreamsNs}#actor")]
	[JC(typeof(ASActorConverter))]
	public ASActor? Actor { get; set; }

	[J($"{Constants.ActivityStreamsNs}#object")]
	[JC(typeof(ASObjectConverter))]
	public ASObject? Object { get; set; }

	public new static class Types
	{
		private const string Ns = Constants.ActivityStreamsNs;

		// Supported AS 2.0 activities
		public const string Create   = $"{Ns}#Create";
		public const string Update   = $"{Ns}#Update";
		public const string Delete   = $"{Ns}#Delete";
		public const string Announce = $"{Ns}#Announce";
		public const string Follow   = $"{Ns}#Follow";
		public const string Unfollow = $"{Ns}#Unfollow";
		public const string Accept   = $"{Ns}#Accept";
		public const string Reject   = $"{Ns}#Reject";
		public const string Undo     = $"{Ns}#Undo";
		public const string Like     = $"{Ns}#Like";
		public const string Block    = $"{Ns}#Block";
		public const string Move     = $"{Ns}#Move";

		// Extensions
		public const string Bite       = "https://ns.mia.jetzt/as#Bite";
		public const string EmojiReact = "http://litepub.social/ns#EmojiReact";
	}
}

public class ASCreate : ASActivity
{
	public ASCreate() => Type = Types.Create;

	[J($"{Constants.ActivityStreamsNs}#to")]
	public List<ASObjectBase>? To { get; set; }

	[J($"{Constants.ActivityStreamsNs}#cc")]
	public List<ASObjectBase>? Cc { get; set; }

	[JI]
	public new ASNote? Object
	{
		get => base.Object as ASNote;
		set
		{
			base.Object = value;
			To          = value?.To;
			Cc          = value?.Cc;
		}
	}
}

public class ASAnnounce : ASActivity
{
	public ASAnnounce() => Type = Types.Announce;

	[J($"{Constants.ActivityStreamsNs}#to")]
	public List<ASObjectBase>? To { get; set; }

	[J($"{Constants.ActivityStreamsNs}#cc")]
	public List<ASObjectBase>? Cc { get; set; }

	public Note.NoteVisibility GetVisibility(User actor)
	{
		if (actor.IsLocalUser) throw new Exception("Can't get recipients for local actor");

		if (To?.Any(p => p.Id == $"{Constants.ActivityStreamsNs}#Public") ?? false)
			return Note.NoteVisibility.Public;
		if (Cc?.Any(p => p.Id == $"{Constants.ActivityStreamsNs}#Public") ?? false)
			return Note.NoteVisibility.Home;
		if (To?.Any(p => p.Id is not null && p.Id == (actor.FollowersUri ?? actor.Uri + "/followers")) ?? false)
			return Note.NoteVisibility.Followers;

		return Note.NoteVisibility.Specified;
	}
}

public class ASDelete : ASActivity
{
	public ASDelete() => Type = Types.Delete;

	[J($"{Constants.ActivityStreamsNs}#to")]
	public List<ASObjectBase>? To { get; set; }
}

public class ASFollow : ASActivity
{
	public ASFollow() => Type = Types.Follow;
}

public class ASUnfollow : ASActivity
{
	public ASUnfollow() => Type = Types.Unfollow;
}

public class ASAccept : ASActivity
{
	public ASAccept() => Type = Types.Accept;
}

public class ASReject : ASActivity
{
	public ASReject() => Type = Types.Reject;
}

public class ASUndo : ASActivity
{
	public ASUndo() => Type = Types.Undo;
}

public class ASBlock : ASActivity
{
	public ASBlock() => Type = Types.Block;
}

public class ASLike : ASActivity
{
	public ASLike() => Type = Types.Like;

	[J($"{Constants.MisskeyNs}#_misskey_reaction")]
	[JC(typeof(VC))]
	public string? MisskeyReaction { get; set; }

	[J($"{Constants.ActivityStreamsNs}#content")]
	[JC(typeof(VC))]
	public string? Content { get; set; }

	[J($"{Constants.ActivityStreamsNs}#tag")]
	[JC(typeof(ASTagConverter))]
	public List<ASTag>? Tags { get; set; }
}

public class ASUpdate : ASActivity
{
	public ASUpdate() => Type = Types.Update;

	[J($"{Constants.ActivityStreamsNs}#to")]
	public List<ASObjectBase>? To { get; set; }

	[J($"{Constants.ActivityStreamsNs}#cc")]
	public List<ASObjectBase>? Cc { get; set; }

	[JI]
	public new ASObject? Object
	{
		get => base.Object;
		set
		{
			base.Object = value;
			if (value is not ASNote note) return;
			To = note.To;
			Cc = note.Cc;
		}
	}
}

// ASActivity extension as defined on https://ns.mia.jetzt/as/#Bite
public class ASBite : ASActivity
{
	public ASBite() => Type = Types.Bite;

	[JR]
	[J($"{Constants.ActivityStreamsNs}#target")]
	[JC(typeof(ASObjectBaseConverter))]
	public required ASObjectBase Target { get; set; }

	[J($"{Constants.ActivityStreamsNs}#to")]
	[JC(typeof(ASObjectBaseConverter))]
	public ASObjectBase? To { get; set; }

	[J($"{Constants.ActivityStreamsNs}#published")]
	[JC(typeof(VC))]
	public DateTime? PublishedAt { get; set; }
}

public class ASEmojiReact : ASActivity
{
	public ASEmojiReact() => Type = Types.EmojiReact;

	[JR]
	[J($"{Constants.ActivityStreamsNs}#content")]
	[JC(typeof(VC))]
	public required string Content { get; set; }

	[J($"{Constants.ActivityStreamsNs}#tag")]
	[JC(typeof(ASTagConverter))]
	public List<ASTag>? Tags { get; set; }
}

public class ASMove : ASActivity
{
	public ASMove() => Type = Types.Move;

	[JR]
	[J($"{Constants.ActivityStreamsNs}#target")]
	[JC(typeof(ASLinkConverter))]
	public required ASLink Target { get; set; }
}