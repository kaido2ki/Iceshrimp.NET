using J = Newtonsoft.Json.JsonPropertyAttribute;
using JC = Newtonsoft.Json.JsonConverterAttribute;

namespace Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;

public class ASEndpoints {
	[J("https://www.w3.org/ns/activitystreams#sharedInbox")]
	[JC(typeof(LDIdObjectConverter))]
	public LDIdObject? SharedInbox { get; set; }
}

public class ASEndpointsConverter : ASSerializer.ListSingleObjectConverter<ASEndpoints>;