using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net;
using Iceshrimp.Backend.Controllers.Mastodon.Attributes;
using Iceshrimp.Backend.Controllers.Mastodon.Schemas;
using Iceshrimp.Backend.Controllers.Schemas;
using Iceshrimp.Backend.Core.Configuration;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Core.Middleware;

public class RequestDurationMiddleware : IMiddleware {
	public async Task InvokeAsync(HttpContext ctx, RequestDelegate next) {
		if (ctx.GetEndpoint()?.Metadata.GetMetadata<HideRequestDuration>() == null) {
			var pre = DateTime.Now;
			ctx.Response.OnStarting(() => {
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