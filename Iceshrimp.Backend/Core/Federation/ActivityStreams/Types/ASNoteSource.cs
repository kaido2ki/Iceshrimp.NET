using Iceshrimp.Backend.Core.Configuration;
using J = Newtonsoft.Json.JsonPropertyAttribute;
using JC = Newtonsoft.Json.JsonConverterAttribute;
using VC = Iceshrimp.Backend.Core.Federation.ActivityStreams.Types.ValueObjectConverter;
using LVC = Iceshrimp.Backend.Core.Federation.ActivityStreams.Types.LocalizedValueObjectConverter;

namespace Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;

public class ASNoteSource
{
	[J($"{Constants.ActivityStreamsNs}#content")]
	[JC(typeof(LVC))]
	public LDLocalizedString? Content { get; set; }

	[J($"{Constants.ActivityStreamsNs}#mediaType")]
	[JC(typeof(VC))]
	public string? MediaType { get; set; }
}

public class ASNoteSourceConverter : ASSerializer.ListSingleObjectConverter<ASNoteSource>;