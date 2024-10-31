using System.Net;
using System.Security.Cryptography;
using System.Text;
using Iceshrimp.Backend.Core.Federation.ActivityStreams;
using Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;
using Iceshrimp.Backend.Core.Federation.Cryptography;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Backend.Core.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;

namespace Iceshrimp.Tests.Cryptography;

[TestClass]
public class HttpSignatureTests
{
	private readonly ASActor _actor    = MockObjects.ASActor;
	private          JArray  _expanded = null!;

	[TestInitialize]
	public void Initialize()
	{
		_expanded = LdHelpers.Expand(_actor);
		_expanded.Should().NotBeNull();
	}

	[TestMethod]
	public async Task SignedGetTest()
	{
		var provider = MockObjects.ServiceProvider;

		var httpRqSvc = provider.GetService<HttpRequestService>();
		var request = httpRqSvc!.GetSigned("https://example.org/users/1234", ["application/ld+json"],
		                                   MockObjects.User, MockObjects.UserKeypair);

		var verify = await request.VerifyAsync(MockObjects.UserKeypair.PublicKey);
		verify.Should().BeTrue();
	}

	[TestMethod]
	public async Task InvalidSignatureDateTest()
	{
		var provider = MockObjects.ServiceProvider;

		var httpRqSvc = provider.GetService<HttpRequestService>();
		var request = httpRqSvc!.GetSigned("https://example.org/users/1234", ["application/ld+json"],
		                                   MockObjects.User, MockObjects.UserKeypair);

		request.Headers.Date = DateTimeOffset.Now - TimeSpan.FromHours(13);

		var task = request.VerifyAsync(MockObjects.UserKeypair.PublicKey);
		var e    = await Assert.ThrowsExceptionAsync<GracefulException>(() => task);
		e.StatusCode.Should().Be(HttpStatusCode.Forbidden);
		e.Message.Should().Be("Request signature is too old");
		e.Error.Should().Be("Forbidden");
	}

	[TestMethod]
	public async Task InvalidSignatureTest()
	{
		var provider = MockObjects.ServiceProvider;

		var httpRqSvc = provider.GetService<HttpRequestService>();
		var request = httpRqSvc!.GetSigned("https://example.org/users/1234", ["application/ld+json"],
		                                   MockObjects.User, MockObjects.UserKeypair);

		var sig = request.Headers.GetValues("Signature").First();
		sig = new StringBuilder(sig) { [sig.Length - 10] = (char)(sig[^10] % (122 - 96) + 97) }
			.ToString();

		request.Headers.Remove("Signature");
		request.Headers.Add("Signature", sig);

		var verify = await request.VerifyAsync(MockObjects.UserKeypair.PublicKey);
		verify.Should().BeFalse();
	}

	[TestMethod]
	public async Task ModifiedUriTest()
	{
		var provider = MockObjects.ServiceProvider;

		var httpRqSvc = provider.GetService<HttpRequestService>();
		var request = httpRqSvc!.GetSigned("https://example.org/users/1234", ["application/ld+json"],
		                                   MockObjects.User, MockObjects.UserKeypair);

		request.RequestUri = new Uri(request.RequestUri + "5");

		var verify = await request.VerifyAsync(MockObjects.UserKeypair.PublicKey);
		verify.Should().BeFalse();
	}

	[TestMethod]
	public async Task SignedPostTest()
	{
		var provider = MockObjects.ServiceProvider;

		var httpRqSvc = provider.GetService<HttpRequestService>();
		var request = await httpRqSvc!.PostSignedAsync("https://example.org/users/1234", "body", "text/plain",
		                                               MockObjects.User, MockObjects.UserKeypair);

		var verify = await request.VerifyAsync(MockObjects.UserKeypair.PublicKey);
		verify.Should().BeTrue();
	}

	[TestMethod]
	public async Task ModifiedBodyTest()
	{
		var provider = MockObjects.ServiceProvider;

		var httpRqSvc = provider.GetService<HttpRequestService>();
		var request = await httpRqSvc!.PostSignedAsync("https://example.org/users/1234", "body", "text/plain",
		                                               MockObjects.User, MockObjects.UserKeypair);

		request.Content = new StringContent("modified-body");

		var verify = await request.VerifyAsync(MockObjects.UserKeypair.PublicKey);
		verify.Should().BeFalse();
	}

	[TestMethod]
	public async Task PseudoHeaderTest()
	{
		const string keyId   = "https://example.org/users/user#main-key";
		const string algo    = "hs2019";
		const string headers = "(request-target) host (created) (expires) (opaque)";
		const string opaque  = "stub";

		var created = (int)(DateTime.UtcNow - TimeSpan.FromSeconds(5) - DateTime.UnixEpoch).TotalSeconds;
		var expires = (int)(DateTime.UtcNow + TimeSpan.FromHours(1) - DateTime.UnixEpoch).TotalSeconds;

		var sigHeader =
			$"keyId=\"{keyId}\",algorithm=\"{algo}\",created=\"{created}\",expires=\"{expires}\",headers=\"{headers}\",opaque=\"{opaque}\",signature=\"stub\"";

		var parsed = HttpSignature.Parse(sigHeader);
		var dict   = new HeaderDictionary { { "host", "example.org" } };
		var signingString =
			HttpSignature.GenerateSigningString(headers.Split(" "), "GET", "/", dict, "example.org", parsed);

		var keypair = MockObjects.UserKeypair;
		var rsa     = RSA.Create();
		rsa.ImportFromPem(keypair.PrivateKey);
		var signatureBytes = rsa.SignData(Encoding.UTF8.GetBytes(signingString), HashAlgorithmName.SHA256,
		                                  RSASignaturePadding.Pkcs1);
		sigHeader = sigHeader.Replace("signature=\"stub\"", $"signature=\"{Convert.ToBase64String(signatureBytes)}\"");
		parsed    = HttpSignature.Parse(sigHeader);

		parsed.KeyId.Should().Be(keyId);
		parsed.Algo.Should().Be(algo);
		parsed.Opaque.Should().Be(opaque);
		parsed.Created.Should().Be(created.ToString());
		parsed.Expires.Should().Be(expires.ToString());
		parsed.Headers.Should().BeEquivalentTo(headers.Split(' '));
		parsed.Signature.Should().BeEquivalentTo(signatureBytes);

		var res = await HttpSignature.VerifySignatureAsync(keypair.PublicKey, signingString, parsed, dict, null);
		res.Should().BeTrue();
	}

	[TestMethod]
	public async Task PseudoHeaderExpiredTest()
	{
		const string keyId   = "https://example.org/users/user#main-key";
		const string algo    = "hs2019";
		const string headers = "(request-target) host (created) (expires) (opaque)";
		const string opaque  = "stub";

		var created = (int)(DateTime.UtcNow - TimeSpan.FromSeconds(5) - DateTime.UnixEpoch).TotalSeconds;
		var expires = (int)(DateTime.UtcNow - TimeSpan.FromHours(1) - DateTime.UnixEpoch).TotalSeconds;

		var sigHeader =
			$"keyId=\"{keyId}\",algorithm=\"{algo}\",created=\"{created}\",expires=\"{expires}\",headers=\"{headers}\",opaque=\"{opaque}\",signature=\"stub\"";

		var parsed = HttpSignature.Parse(sigHeader);
		var dict   = new HeaderDictionary { { "host", "example.org" } };
		var signingString =
			HttpSignature.GenerateSigningString(headers.Split(" "), "GET", "/", dict, "example.org", parsed);

		var keypair = MockObjects.UserKeypair;
		var rsa     = RSA.Create();
		rsa.ImportFromPem(keypair.PrivateKey);
		var signatureBytes = rsa.SignData(Encoding.UTF8.GetBytes(signingString), HashAlgorithmName.SHA256,
		                                  RSASignaturePadding.Pkcs1);
		sigHeader = sigHeader.Replace("signature=\"stub\"", $"signature=\"{Convert.ToBase64String(signatureBytes)}\"");
		parsed    = HttpSignature.Parse(sigHeader);

		var task = HttpSignature.VerifySignatureAsync(keypair.PublicKey, signingString, parsed, dict, null);
		var ex   = await Assert.ThrowsExceptionAsync<GracefulException>(() => task);
		ex.Message.Should().Be("Request signature is expired");
	}

	[TestMethod]
	public async Task PseudoHeaderTooOldTest()
	{
		const string keyId   = "https://example.org/users/user#main-key";
		const string algo    = "hs2019";
		const string headers = "(request-target) host (created) (expires) (opaque)";
		const string opaque  = "stub";

		var created = (int)(DateTime.UtcNow - TimeSpan.FromHours(24) - DateTime.UnixEpoch).TotalSeconds;
		var expires = (int)(DateTime.UtcNow + TimeSpan.FromHours(1) - DateTime.UnixEpoch).TotalSeconds;

		var sigHeader =
			$"keyId=\"{keyId}\",algorithm=\"{algo}\",created=\"{created}\",expires=\"{expires}\",headers=\"{headers}\",opaque=\"{opaque}\",signature=\"stub\"";

		var parsed = HttpSignature.Parse(sigHeader);
		var dict   = new HeaderDictionary { { "host", "example.org" } };
		var signingString =
			HttpSignature.GenerateSigningString(headers.Split(" "), "GET", "/", dict, "example.org", parsed);

		var keypair = MockObjects.UserKeypair;
		var rsa     = RSA.Create();
		rsa.ImportFromPem(keypair.PrivateKey);
		var signatureBytes = rsa.SignData(Encoding.UTF8.GetBytes(signingString), HashAlgorithmName.SHA256,
		                                  RSASignaturePadding.Pkcs1);
		sigHeader = sigHeader.Replace("signature=\"stub\"", $"signature=\"{Convert.ToBase64String(signatureBytes)}\"");
		parsed    = HttpSignature.Parse(sigHeader);

		var task = HttpSignature.VerifySignatureAsync(keypair.PublicKey, signingString, parsed, dict, null);
		var ex   = await Assert.ThrowsExceptionAsync<GracefulException>(() => task);
		ex.Message.Should().Be("Request signature is too old");
	}
}