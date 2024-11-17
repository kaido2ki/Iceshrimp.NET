using System.Diagnostics;
using Iceshrimp.Backend.Core.Extensions;
using JetBrains.Annotations;

namespace Iceshrimp.Backend.Core.Middleware;

[UsedImplicitly]
public class RequestDurationMiddleware(RequestDelegate next)
{
	[UsedImplicitly]
	public async Task InvokeAsync(HttpContext ctx)
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