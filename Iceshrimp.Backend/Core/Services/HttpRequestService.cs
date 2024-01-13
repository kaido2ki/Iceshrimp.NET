using System.Net.Http.Headers;
using Iceshrimp.Backend.Core.Configuration;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Core.Services;

public class HttpRequestService(IOptions<Config.InstanceSection> options) {
	private HttpRequestMessage GenerateRequest(string url, IEnumerable<string>? accept, HttpMethod method) {
		var message = new HttpRequestMessage {
			RequestUri = new Uri(url),
			Method     = method,
			//Headers    = { UserAgent = { ProductInfoHeaderValue.Parse(options.Value.UserAgent) } }
		};

		//TODO: fix the user-agent so the commented out bit above works
		message.Headers.TryAddWithoutValidation("User-Agent", options.Value.UserAgent);

		if (accept != null) {
			foreach (var type in accept.Select(MediaTypeWithQualityHeaderValue.Parse))
				message.Headers.Accept.Add(type);
		}

		return message;
	}

	public HttpRequestMessage Get(string url, IEnumerable<string>? accept) {
		return GenerateRequest(url, accept, HttpMethod.Get);
	}
}