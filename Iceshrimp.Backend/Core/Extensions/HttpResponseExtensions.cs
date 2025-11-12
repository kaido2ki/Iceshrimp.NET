using System.Net;

namespace Iceshrimp.Backend.Core.Extensions;

public static class HttpResponseExtensions
{
	extension(HttpResponseMessage res)
	{
		public bool IsClientError()
			=> res.StatusCode is >= HttpStatusCode.BadRequest and <= (HttpStatusCode)499;

		public bool IsRetryableClientError()
			=> res.StatusCode is HttpStatusCode.TooManyRequests;

		public void EnsureSuccessStatusCode(
			bool excludeClientErrors, Func<Exception> exceptionFactory
		)
		{
			if (excludeClientErrors && res.IsClientError() && !res.IsRetryableClientError())
				throw exceptionFactory();
			res.EnsureSuccessStatusCode();
		}
	}
}