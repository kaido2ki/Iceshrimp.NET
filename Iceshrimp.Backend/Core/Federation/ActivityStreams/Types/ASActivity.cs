using Iceshrimp.Backend.Core.Configuration;
using J = Newtonsoft.Json.JsonPropertyAttribute;
using JC = Newtonsoft.Json.JsonConverterAttribute;

namespace Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;

public class ASActivity : ASObject {
	[J("https://www.w3.org/ns/activitystreams#actor")]
	[JC(typeof(ASActorConverter))]
	public ASActor? Actor { get; set; }

	[J("https://www.w3.org/ns/activitystreams#object")]
	[JC(typeof(ASObjectConverter))]
	public ASObject? Object { get; set; }

	public new static class Types {
		private const string Ns = Constants.ActivityStreamsNs;

		public const string Create   = $"{Ns}#Create";
		public const string Delete   = $"{Ns}#Delete";
		public const string Follow   = $"{Ns}#Follow";
		public const string Unfollow = $"{Ns}#Unfollow";
		public const string Accept   = $"{Ns}#Accept";
		public const string Reject   = $"{Ns}#Reject";
		public const string Undo     = $"{Ns}#Undo";
		public const string Like     = $"{Ns}#Like";
	}
}

public class ASCreate : ASActivity {
	public ASCreate() => Type = Types.Create;
}

public class ASDelete : ASActivity {
	public ASDelete() => Type = Types.Delete;
}

public class ASFollow : ASActivity {
	public ASFollow() => Type = Types.Follow;
}

public class ASUnfollow : ASActivity {
	public ASUnfollow() => Type = Types.Unfollow;
}

public class ASAccept : ASActivity {
	public ASAccept() => Type = Types.Accept;
}

public class ASReject : ASActivity {
	public ASReject() => Type = Types.Reject;
}

public class ASUndo : ASActivity {
	public ASUndo() => Type = Types.Undo;
}

//TODO: add the rest

public sealed class ASActivityConverter : ASSerializer.ListSingleObjectConverter<ASActivity>;