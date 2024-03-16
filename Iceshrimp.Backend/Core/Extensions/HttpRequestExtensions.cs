namespace Iceshrimp.Backend.Core.Extensions;

public static class HttpRequestExtensions
{
	public static bool HasBody(this HttpRequest request) =>
		request.ContentLength > 0 || request.Headers.TransferEncoding.FirstOrDefault() == "chunked";
}