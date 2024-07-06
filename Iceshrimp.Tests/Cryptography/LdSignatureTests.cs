using System.Security.Cryptography;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Federation.ActivityStreams;
using Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;
using Iceshrimp.Backend.Core.Federation.Cryptography;
using Newtonsoft.Json.Linq;

namespace Iceshrimp.Tests.Cryptography;

[TestClass]
public class LdSignatureTests
{
	private readonly ASActor _actor    = MockObjects.ASActor;
	private readonly RSA     _keypair  = MockObjects.Keypair;
	private          JArray  _expanded = null!;
	private          JObject _signed   = null!;

	[TestInitialize]
	public async Task Initialize()
	{
		_expanded = LdHelpers.Expand(_actor);
		_signed   = await LdSignature.SignAsync(_expanded, _keypair.ExportRSAPrivateKeyPem(), _actor.Id + "#main-key");

		_expanded.Should().NotBeNull();
		_signed.Should().NotBeNull();
	}

	[TestMethod]
	public async Task RoundtripTest()
	{
		var verify = await LdSignature.VerifyAsync(_signed, _signed, _keypair.ExportRSAPublicKeyPem());
		verify.Should().BeTrue();
	}

	[TestMethod]
	public async Task InvalidKeyTest()
	{
		var rsa    = RSA.Create();
		var verify = await LdSignature.VerifyAsync(_signed, _signed, rsa.ExportRSAPublicKeyPem());
		verify.Should().BeFalse();
	}

	[TestMethod]
	public async Task InvalidDataTest()
	{
		var data = (_signed.DeepClone() as JObject)!;
		data.Should().NotBeNull();

		data.Add("https://example.org/ns#test", JToken.FromObject("value"));
		var expanded = LdHelpers.Expand(data);
		expanded.Should().NotBeNull();

		var verify = await LdSignature.VerifyAsync(expanded, expanded, _keypair.ExportRSAPublicKeyPem());
		verify.Should().BeFalse();
	}

	[TestMethod]
	public async Task MissingSignatureTest()
	{
		var data = (_signed.DeepClone() as JObject)!;
		data.Should().NotBeNull();

		data.Remove($"{Constants.W3IdSecurityNs}#signature");
		var verify = await LdSignature.VerifyAsync(data, data, _keypair.ExportRSAPublicKeyPem());
		verify.Should().BeFalse();
	}

	[TestMethod]
	public async Task InvalidSignatureTest()
	{
		var data = (_signed.DeepClone() as JObject)!;
		data.Should().NotBeNull();

		var signature =
			data[$"{Constants.W3IdSecurityNs}#signature"]?[0]?[$"{Constants.W3IdSecurityNs}#signatureValue"]?[0]?
				["@value"];

		signature.Should().NotBeNull();

		data[$"{Constants.W3IdSecurityNs}#signature"]![0]![$"{Constants.W3IdSecurityNs}#signatureValue"]![0]!
			["@value"] += "test";
		var e = await Assert.ThrowsExceptionAsync<FormatException>(async () => await LdSignature.VerifyAsync(data, data,
				                                                            _keypair.ExportRSAPublicKeyPem()));

		e.Message.Should()
		 .Be("The input is not a valid Base-64 string as it contains a non-base 64 character, more than two padding characters, or an illegal character among the padding characters.");
	}

	[TestMethod]
	public async Task InvalidSignatureOptionsTest()
	{
		var data = (_signed.DeepClone() as JObject)!;
		data.Should().NotBeNull();

		var creator =
			data[$"{Constants.W3IdSecurityNs}#signature"]?[0]?["http://purl.org/dc/terms/creator"]?[0]?["@id"];
		creator.Should().NotBeNull();

		data[$"{Constants.W3IdSecurityNs}#signature"]![0]!["http://purl.org/dc/terms/creator"]![0]!["@id"] += "test";
		var verify = await LdSignature.VerifyAsync(data, data, _keypair.ExportRSAPublicKeyPem());
		verify.Should().BeFalse();
	}
}