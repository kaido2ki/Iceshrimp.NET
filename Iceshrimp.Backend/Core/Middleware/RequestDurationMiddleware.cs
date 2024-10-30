using System.Diagnostics;
using Iceshrimp.Backend.Core.Extensions;

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
				var duration = Stopwatch.GetElapsedTime(pre).GetTotalMilliseconds();
				ctx.Response.Headers.Append("X-Request-Duration", $"{duration} ms");
				return Task.CompletedTask;
			});
		}

		await next(ctx);
	}
}

public class HideRequestDuration : Attribute;