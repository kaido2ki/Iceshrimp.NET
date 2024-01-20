using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
}

public sealed class ASActivityConverter : ASSerializer.ListSingleObjectConverter<ASActivity>;