using System.Security.Cryptography;
using FluentAssertions;
using Iceshrimp.Backend.Core.Federation.ActivityStreams;
using Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;
using Iceshrimp.Backend.Core.Federation.Cryptography;
using Iceshrimp.Backend.Core.Helpers;

namespace Iceshrimp.Tests.Cryptography;

[TestClass]
public class LdSignatureTests {
	[TestMethod]
	public async Task RoundtripTest() {
		var keypair = RSA.Create();

		var actor = new ASActor {
			Id             = $"https://example.org/users/{IdHelpers.GenerateSlowflakeId()}",
			Type           = ["https://www.w3.org/ns/activitystreams#Person"],
			Url            = new ASLink($"https://example.org/@test"),
			Username       = "test",
			DisplayName    = "Test account",
			IsCat          = false,
			IsDiscoverable = true,
			IsLocked       = true
		};

		var expanded  = LdHelpers.Expand(actor);

		var signed = await LdSignature.Sign(expanded!, keypair.ExportRSAPrivateKeyPem(), actor.Id + "#main-key");
		var verify = await LdSignature.Verify(signed, keypair.ExportRSAPublicKeyPem());

		verify.Should().BeTrue();
	}
}