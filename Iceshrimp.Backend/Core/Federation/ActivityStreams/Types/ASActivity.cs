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

	public static class Types {
		private const string Ns = "https://www.w3.org/ns/activitystreams";

		public const string Create   = $"{Ns}#Create";
		public const string Delete   = $"{Ns}#Delete";
		public const string Follow   = $"{Ns}#Follow";
		public const string Unfollow = $"{Ns}#Unfollow";
		public const string Accept   = $"{Ns}#Accept";
		public const string Undo     = $"{Ns}#Undo";
		public const string Like     = $"{Ns}#Like";
	}
}

public sealed class ASActivityConverter : ASSerializer.ListSingleObjectConverter<ASActivity>;