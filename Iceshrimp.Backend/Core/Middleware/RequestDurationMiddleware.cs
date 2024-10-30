using System.Diagnostics;
using System.Globalization;

namespace Iceshrimp.Backend.Core.Middleware;

public class RequestDurationMiddleware : IMiddleware
{
	public async Task InvokeAsync(HttpContext ctx, RequestDelegate next)
	{
		if (ctx.GetEndpoint()?.Metadata.GetMetadata<HideRequestDuration>() == null)
		{
			var pre = Stopwatch.GetTimestamp();
			ctx.Response.OnStarting(() =>
			{
				var duration = Math.Truncate(Stopwatch.GetElapsedTime(pre).TotalMilliseconds);
				ctx.Response.Headers.Append("X-Request-Duration",
				                            duration.ToString(CultureInfo.InvariantCulture) + " ms");
				return Task.CompletedTask;
			});
		}

		await next(ctx);
	}
}

public class HideRequestDuration : Attribute;