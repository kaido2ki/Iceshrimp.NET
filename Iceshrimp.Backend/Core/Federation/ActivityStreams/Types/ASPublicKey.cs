using J = Newtonsoft.Json.JsonPropertyAttribute;
using JC = Newtonsoft.Json.JsonConverterAttribute;
using VC = Iceshrimp.Backend.Core.Federation.ActivityStreams.Types.ValueObjectConverter;

namespace Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;

public class ASPublicKey : ASObject {
	[J("https://w3id.org/security#owner")]
	[JC(typeof(ASObjectBaseConverter))]
	public ASObjectBase? Owner { get; set; }
	
	[J("https://w3id.org/security#publicKeyPem")]
	[JC(typeof(VC))]
	public string? PublicKey { get; set; }
}

public class ASPublicKeyConverter : ASSerializer.ListSingleObjectConverter<ASPublicKey>;