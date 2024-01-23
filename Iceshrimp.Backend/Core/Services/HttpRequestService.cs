using System.Net.Http.Headers;
using System.Security.Cryptography;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Federation.Cryptography;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Core.Services;

public class HttpRequestService(IOptions<Config.InstanceSection> options) {
	private HttpRequestMessage GenerateRequest(string url, HttpMethod method,
	                                           string? body = null,
	                                           string? contentType = null,
	                                           IEnumerable<string>? accept = null) {
		var message = new HttpRequestMessage {
			RequestUri = new Uri(url),
			Method     = method,
			//Headers    = { UserAgent = { ProductInfoHeaderValue.Parse(options.Value.UserAgent) } }
		};

		//TODO: fix the user-agent so the commented out bit above works
		message.Headers.TryAddWithoutValidation("User-Agent", options.Value.UserAgent);

		if (body != null) {
			ArgumentNullException.ThrowIfNull(contentType);
			message.Content = new StringContent(body, MediaTypeHeaderValue.Parse(contentType));
		}

		if (accept != null) {
			foreach (var type in accept.Select(MediaTypeWithQualityHeaderValue.Parse))
				message.Headers.Accept.Add(type);
		}

		return message;
	}

	public HttpRequestMessage Get(string url, IEnumerable<string>? accept) {
		return GenerateRequest(url, HttpMethod.Get, accept: accept);
	}

	public HttpRequestMessage Post(string url, string body, string contentType) {
		return GenerateRequest(url, HttpMethod.Post, body, contentType);
	}

	public HttpRequestMessage GetSigned(string url, IEnumerable<string>? accept, User user,
	                                    UserKeypair keypair) {
		return Get(url, accept).Sign(["(request-target)", "date", "host", "accept"], keypair.PrivateKey,
		                             $"https://{options.Value.WebDomain}/users/{user.Id}#main-key");
	}

	public async Task<HttpRequestMessage> PostSigned(string url, string body, string contentType, User user,
	                                                 UserKeypair keypair) {
		var message = Post(url, body, contentType);
		ArgumentNullException.ThrowIfNull(message.Content);

		// Generate and attach digest header
		var content = await message.Content.ReadAsStreamAsync();
		var digest  = await SHA256.HashDataAsync(content);
		message.Headers.Add("Digest", Convert.ToBase64String(digest));

		// Return the signed message
		return message.Sign(["(request-target)", "date", "host", "digest"], keypair.PrivateKey,
		                    $"https://{options.Value.WebDomain}/users/{user.Id}#main-key");
	}
}