using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Federation.Cryptography;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Core.Services;

public class HttpRequestService(IOptions<Config.InstanceSection> options)
{
	private static HttpRequestMessage GenerateRequest(
		string url, HttpMethod method,
		string? body = null,
		string? contentType = null,
		IEnumerable<string>? accept = null
	)
	{
		var message = new HttpRequestMessage
		{
			RequestUri = new Uri(url),
			Method     = method,

			// Default to HTTP/2, but allow for down-negotiation to HTTP/1.1 or HTTP/1.0
			Version       = HttpVersion.Version20,
			VersionPolicy = HttpVersionPolicy.RequestVersionOrLower
		};

		if (body != null)
		{
			ArgumentNullException.ThrowIfNull(contentType);
			message.Content = new StringContent(body, MediaTypeHeaderValue.Parse(contentType));
		}

		if (accept != null)
			foreach (var type in accept.Select(MediaTypeWithQualityHeaderValue.Parse))
				message.Headers.Accept.Add(type);

		return message;
	}

	public HttpRequestMessage Get(string url, IEnumerable<string>? accept)
	{
		return GenerateRequest(url, HttpMethod.Get, accept: accept);
	}

	public HttpRequestMessage Post(string url, string body, string contentType)
	{
		return GenerateRequest(url, HttpMethod.Post, body, contentType);
	}

	public HttpRequestMessage GetSigned(
		string url, IEnumerable<string>? accept, string actorId,
		string privateKey
	)
	{
		return Get(url, accept)
			.Sign(["(request-target)", "date", "host", "accept"], privateKey,
			      $"https://{options.Value.WebDomain}/users/{actorId}#main-key");
	}

	public HttpRequestMessage GetSigned(
		string url, IEnumerable<string>? accept, User actor,
		UserKeypair keypair
	)
	{
		return GetSigned(url, accept, actor.Id, keypair.PrivateKey);
	}

	public async Task<HttpRequestMessage> PostSignedAsync(
		string url, string body, string contentType, string actorId,
		string privateKey
	)
	{
		var message = Post(url, body, contentType);
		ArgumentNullException.ThrowIfNull(message.Content);

		// Generate and attach digest header
		var content = await message.Content.ReadAsStreamAsync();
		var digest  = await SHA256.HashDataAsync(content);
		message.Headers.Add("Digest", "SHA-256=" + Convert.ToBase64String(digest));

		// Return the signed message
		return message.Sign(["(request-target)", "date", "host", "digest"], privateKey,
		                    $"https://{options.Value.WebDomain}/users/{actorId}#main-key");
	}

	public Task<HttpRequestMessage> PostSignedAsync(
		string url, string body, string contentType, User actor,
		UserKeypair keypair
	)
	{
		return PostSignedAsync(url, body, contentType, actor.Id, keypair.PrivateKey);
	}
}