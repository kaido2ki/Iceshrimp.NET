using J = Newtonsoft.Json.JsonPropertyAttribute;
using JC = Newtonsoft.Json.JsonConverterAttribute;
using VC = Iceshrimp.Backend.Core.Federation.ActivityStreams.Types.ValueObjectConverter;

namespace Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;

public class ASNoteSource {
	[J("https://www.w3.org/ns/activitystreams#content")]
	[JC(typeof(VC))]
	public string? Content { get; set; }

	[J("https://www.w3.org/ns/activitystreams#mediaType")]
	[JC(typeof(VC))]
	public string? MediaType { get; set; }
}

public class ASNoteSourceConverter : ASSerializer.ListSingleObjectConverter<ASNoteSource>;