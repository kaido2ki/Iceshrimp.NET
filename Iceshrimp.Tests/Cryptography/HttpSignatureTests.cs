using System.Net;
using System.Text;
using Iceshrimp.Backend.Core.Federation.ActivityStreams;
using Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;
using Iceshrimp.Backend.Core.Federation.Cryptography;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Backend.Core.Services;
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
		_expanded = LdHelpers.Expand(_actor)!;
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

		var e = await Assert.ThrowsExceptionAsync<GracefulException>(async () =>
			                                                             await request.VerifyAsync(MockObjects
						                                                              .UserKeypair
						                                                              .PublicKey));
		e.StatusCode.Should().Be(HttpStatusCode.Forbidden);
		e.Message.Should().Be("Request signature too old");
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
}