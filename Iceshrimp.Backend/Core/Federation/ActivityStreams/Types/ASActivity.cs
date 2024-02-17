using Iceshrimp.Backend.Core.Configuration;
using J = Newtonsoft.Json.JsonPropertyAttribute;
using JC = Newtonsoft.Json.JsonConverterAttribute;
using JI = Newtonsoft.Json.JsonIgnoreAttribute;

namespace Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;

public class ASActivity : ASObject
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

		public const string Create   = $"{Ns}#Create";
		public const string Update   = $"{Ns}#Update";
		public const string Delete   = $"{Ns}#Delete";
		public const string Follow   = $"{Ns}#Follow";
		public const string Unfollow = $"{Ns}#Unfollow";
		public const string Accept   = $"{Ns}#Accept";
		public const string Reject   = $"{Ns}#Reject";
		public const string Undo     = $"{Ns}#Undo";
		public const string Like     = $"{Ns}#Like";
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

public class ASDelete : ASActivity
{
	public ASDelete() => Type = Types.Delete;
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

public class ASLike : ASActivity
{
	public ASLike() => Type = Types.Like;
}

public class ASUpdate : ASActivity
{
	public ASUpdate() => Type = Types.Update;

	[J($"{Constants.ActivityStreamsNs}#to")]
	public List<ASObjectBase>? To { get; set; }

	[J($"{Constants.ActivityStreamsNs}#cc")]
	public List<ASObjectBase>? Cc { get; set; }

	[J($"{Constants.ActivityStreamsNs}#object")]
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

//TODO: add the rest

public sealed class ASActivityConverter : ASSerializer.ListSingleObjectConverter<ASActivity>;