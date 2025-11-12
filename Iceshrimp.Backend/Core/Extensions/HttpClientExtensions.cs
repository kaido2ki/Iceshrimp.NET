namespace Iceshrimp.Backend.Core.Extensions;

public static class HttpClientExtensions
{
	private static readonly HttpRequestOptionsKey<bool?> AutoRedirectOptionsKey = new("RequestAutoRedirect");

	extension(HttpRequestMessage request)
	{
		public HttpRequestMessage DisableAutoRedirects()
		{
			request.SetAutoRedirect(false);
			return request;
		}

		private void SetAutoRedirect(bool autoRedirect)
		{
			request.Options.Set(AutoRedirectOptionsKey, autoRedirect);
		}

		public bool? GetAutoRedirect()
		{
			request.Options.TryGetValue(AutoRedirectOptionsKey, out var value);
			return value;
		}
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