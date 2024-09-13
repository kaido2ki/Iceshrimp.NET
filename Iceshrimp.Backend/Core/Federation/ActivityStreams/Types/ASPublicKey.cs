using Iceshrimp.Backend.Core.Configuration;
using J = Newtonsoft.Json.JsonPropertyAttribute;
using JR = Newtonsoft.Json.JsonRequiredAttribute;
using JC = Newtonsoft.Json.JsonConverterAttribute;
using VC = Iceshrimp.Backend.Core.Federation.ActivityStreams.Types.ValueObjectConverter;

namespace Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;

public class ASPublicKey : ASObject
{
	[J("@id")]
	[JR]
	public new required string Id
	{
		get => base.Id ?? throw new NullReferenceException("base.Id should never be null on a required property");
		set => base.Id = value;
	}

	[J($"{Constants.W3IdSecurityNs}#owner")]
	[JC(typeof(ASObjectBaseConverter))]
	public ASObjectBase? Owner { get; set; }

	[J($"{Constants.W3IdSecurityNs}#publicKeyPem")]
	[JC(typeof(VC))]
	public string? PublicKey { get; set; }
}

public class ASPublicKeyConverter : ASSerializer.ListSingleObjectConverter<ASPublicKey>;