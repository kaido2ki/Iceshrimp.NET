using Iceshrimp.Backend.Core.Configuration;
using J = Newtonsoft.Json.JsonPropertyAttribute;
using JC = Newtonsoft.Json.JsonConverterAttribute;

namespace Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;

public class ASEndpoints
{
	[J($"{Constants.ActivityStreamsNs}#sharedInbox")]
	[JC(typeof(ASObjectBaseConverter))]
	public ASObjectBase? SharedInbox { get; set; }
}

public class ASEndpointsConverter : ASSerializer.ListSingleObjectConverter<ASEndpoints>;