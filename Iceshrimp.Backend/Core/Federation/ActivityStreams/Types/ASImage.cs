using Iceshrimp.Backend.Core.Configuration;
using J = Newtonsoft.Json.JsonPropertyAttribute;
using JC = Newtonsoft.Json.JsonConverterAttribute;
using VC = Iceshrimp.Backend.Core.Federation.ActivityStreams.Types.ValueObjectConverter;

namespace Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;

public class ASImage
{
	[J($"{Constants.ActivityStreamsNs}#url")]
	[JC(typeof(ASLinkConverter))]
	public ASLink? Url { get; set; }

	[J($"{Constants.ActivityStreamsNs}#sensitive")]
	[JC(typeof(VC))]
	public bool? Sensitive { get; set; }
}

public class ASImageConverter : ASSerializer.ListSingleObjectConverter<ASImage>;