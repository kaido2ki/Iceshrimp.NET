using Microsoft.AspNetCore.Http.Features;

namespace Iceshrimp.Backend.Core.Middleware;

public class RequestBufferingMiddleware : IMiddleware {
	public async Task InvokeAsync(HttpContext ctx, RequestDelegate next) {
		var endpoint  = ctx.Features.Get<IEndpointFeature>()?.Endpoint;
		var attribute = endpoint?.Metadata.GetMetadata<EnableRequestBufferingAttribute>();

		if (attribute != null) ctx.Request.EnableBuffering(attribute.MaxLength);

		await next(ctx);
	}
}

public class EnableRequestBufferingAttribute(long maxLength) : Attribute {
	internal long MaxLength = maxLength;
}