using Iceshrimp.Backend.Core.Configuration;
using J = Newtonsoft.Json.JsonPropertyAttribute;
using JC = Newtonsoft.Json.JsonConverterAttribute;
using VC = Iceshrimp.Backend.Core.Federation.ActivityStreams.Types.ValueObjectConverter;

namespace Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;

public class ASPublicKey : ASObjectWithId
{
	[J($"{Constants.W3IdSecurityNs}#owner")]
	[JC(typeof(ASObjectBaseConverter))]
	public ASObjectBase? Owner { get; set; }

	[J($"{Constants.W3IdSecurityNs}#publicKeyPem")]
	[JC(typeof(VC))]
	public string? PublicKey { get; set; }
}

public class ASPublicKeyConverter : ASSerializer.ListSingleObjectConverter<ASPublicKey>;