namespace Iceshrimp.Backend.Core.Extensions;

public static class HttpClientExtensions
{
	private static readonly HttpRequestOptionsKey<bool?> AutoRedirectOptionsKey = new("RequestAutoRedirect");

	public static HttpRequestMessage DisableAutoRedirects(this HttpRequestMessage request)
	{
		request.SetAutoRedirect(false);
		return request;
	}

	private static void SetAutoRedirect(this HttpRequestMessage request, bool autoRedirect)
	{
		request.Options.Set(AutoRedirectOptionsKey, autoRedirect);
	}

	public static bool? GetAutoRedirect(this HttpRequestMessage request)
	{
		request.Options.TryGetValue(AutoRedirectOptionsKey, out var value);
		return value;
	}

	public static HttpMessageHandler? GetMostInnerHandler(this HttpMessageHandler? self)
	{
		while (self is DelegatingHandler handler)
		{
			self = handler.InnerHandler;
		}

		return self;
	}
}