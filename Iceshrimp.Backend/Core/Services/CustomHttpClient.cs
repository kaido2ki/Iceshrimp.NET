using Iceshrimp.Backend.Core.Configuration;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Core.Services;

public class CustomHttpClient : HttpClient
{
	public CustomHttpClient(IOptions<Config.InstanceSection> options)
	{
		DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", options.Value.UserAgent);
		Timeout = TimeSpan.FromSeconds(30);
	}
}