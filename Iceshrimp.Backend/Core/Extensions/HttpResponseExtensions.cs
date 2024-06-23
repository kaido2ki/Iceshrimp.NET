using System.Net;

namespace Iceshrimp.Backend.Core.Extensions;

public static class HttpResponseExtensions
{
	public static bool IsClientError(this HttpResponseMessage res)
		=> res.StatusCode is >= HttpStatusCode.BadRequest and <= (HttpStatusCode)499;

	public static bool IsRetryableClientError(this HttpResponseMessage res)
		=> res.StatusCode is HttpStatusCode.TooManyRequests;

	public static void EnsureSuccessStatusCode(
		this HttpResponseMessage res, bool excludeClientErrors, Func<Exception> exceptionFactory
	)
	{
		if (excludeClientErrors && res.IsClientError() && !res.IsRetryableClientError())
			throw exceptionFactory();
		res.EnsureSuccessStatusCode();
	}
}