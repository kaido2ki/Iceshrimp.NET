using J = Newtonsoft.Json.JsonPropertyAttribute;
using JC = Newtonsoft.Json.JsonConverterAttribute;
using VC = Iceshrimp.Backend.Core.Federation.ActivityStreams.Types.ValueObjectConverter;

namespace Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;

public class ASImage {
	[J("https://www.w3.org/ns/activitystreams#url")]
	[JC(typeof(ASLinkConverter))]
	public ASLink? Url { get; set; }

	[J("https://www.w3.org/ns/activitystreams#sensitive")]
	[JC(typeof(VC))]
	public bool? Sensitive { get; set; }
}

public class ASImageConverter : ASSerializer.ListSingleObjectConverter<ASImage>;