using System.Text;
using Iceshrimp.Backend.Core.Federation.ActivityStreams;
using Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;
using Iceshrimp.Backend.Core.Federation.Cryptography;
using Iceshrimp.Backend.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;

namespace Iceshrimp.Tests.Cryptography;

[TestClass]
public class HttpSignatureTests {
	private readonly ASActor _actor    = MockObjects.ASActor;
	private          JArray  _expanded = null!;

	[TestInitialize]
	public void Initialize() {
		_expanded = LdHelpers.Expand(_actor)!;
		_expanded.Should().NotBeNull();
	}

	[TestMethod]
	public async Task SignedGetTest() {
		var provider = MockObjects.ServiceProvider;

		var httpRqSvc = provider.GetService<HttpRequestService>();
		var request = httpRqSvc!.GetSigned("https://example.org/users/1234", ["application/ld+json"],
		                                   MockObjects.User, MockObjects.UserKeypair);

		var verify = await request.Verify(MockObjects.UserKeypair.PublicKey);
		verify.Should().BeTrue();
	}

	[TestMethod]
	public async Task InvalidSignatureDateTest() {
		var provider = MockObjects.ServiceProvider;

		var httpRqSvc = provider.GetService<HttpRequestService>();
		var request = httpRqSvc!.GetSigned("https://example.org/users/1234", ["application/ld+json"],
		                                   MockObjects.User, MockObjects.UserKeypair);

		request.Headers.Date = DateTimeOffset.Now - TimeSpan.FromHours(13);

		await Assert.ThrowsExceptionAsync<Exception>(async () =>
			                                             await request.Verify(MockObjects.UserKeypair.PublicKey));
	}

	[TestMethod]
	public async Task InvalidSignatureTest() {
		var provider = MockObjects.ServiceProvider;

		var httpRqSvc = provider.GetService<HttpRequestService>();
		var request = httpRqSvc!.GetSigned("https://example.org/users/1234", ["application/ld+json"],
		                                   MockObjects.User, MockObjects.UserKeypair);

		var sig = request.Headers.GetValues("Signature").First();
		sig = new StringBuilder(sig) { [sig.Length - 10] = (char)(sig[10] + 1 % char.MaxValue) }.ToString();

		request.Headers.Remove("Signature");
		request.Headers.Add("Signature", sig);

		var verify = await request.Verify(MockObjects.UserKeypair.PublicKey);
		verify.Should().BeFalse();
	}

	[TestMethod]
	public async Task ModifiedUriTest() {
		var provider = MockObjects.ServiceProvider;

		var httpRqSvc = provider.GetService<HttpRequestService>();
		var request = httpRqSvc!.GetSigned("https://example.org/users/1234", ["application/ld+json"],
		                                   MockObjects.User, MockObjects.UserKeypair);

		request.RequestUri = new Uri(request.RequestUri + "5");

		var verify = await request.Verify(MockObjects.UserKeypair.PublicKey);
		verify.Should().BeFalse();
	}

	[TestMethod]
	public async Task SignedPostTest() {
		var provider = MockObjects.ServiceProvider;

		var httpRqSvc = provider.GetService<HttpRequestService>();
		var request = await httpRqSvc!.PostSigned("https://example.org/users/1234", "body", "text/plain",
		                                          MockObjects.User, MockObjects.UserKeypair);

		var verify = await request.Verify(MockObjects.UserKeypair.PublicKey);
		verify.Should().BeTrue();
	}

	[TestMethod]
	public async Task ModifiedBodyTest() {
		var provider = MockObjects.ServiceProvider;

		var httpRqSvc = provider.GetService<HttpRequestService>();
		var request = await httpRqSvc!.PostSigned("https://example.org/users/1234", "body", "text/plain",
		                                          MockObjects.User, MockObjects.UserKeypair);

		request.Content = new StringContent("modified-body");

		var verify = await request.Verify(MockObjects.UserKeypair.PublicKey);
		verify.Should().BeFalse();
	}
}