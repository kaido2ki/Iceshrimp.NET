using System.Globalization;

namespace Iceshrimp.Backend.Core.Middleware;

public class RequestDurationMiddleware : IMiddleware
{
	public async Task InvokeAsync(HttpContext ctx, RequestDelegate next)
	{
		if (ctx.GetEndpoint()?.Metadata.GetMetadata<HideRequestDuration>() == null)
		{
			var pre = DateTime.Now;
			ctx.Response.OnStarting(() =>
			{
				var duration = (int)(DateTime.Now - pre).TotalMilliseconds;
				ctx.Response.Headers.Append("X-Request-Duration",
				                            duration.ToString(CultureInfo.InvariantCulture) + " ms");
				return Task.CompletedTask;
			});
		}

		await next(ctx);
	}
}

public class HideRequestDuration : Attribute;