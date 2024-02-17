using Iceshrimp.Backend.Core.Configuration;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Core.Services;

public class HttpClientWithUserAgent : HttpClient
{
	public HttpClientWithUserAgent(IOptions<Config.InstanceSection> options)
	{
		DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", options.Value.UserAgent);
	}
}