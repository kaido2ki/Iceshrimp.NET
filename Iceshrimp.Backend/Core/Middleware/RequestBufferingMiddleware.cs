namespace Iceshrimp.Backend.Core.Middleware;

public class RequestBufferingMiddleware : IMiddleware {
	public async Task InvokeAsync(HttpContext ctx, RequestDelegate next) {
		var attribute = ctx.GetEndpoint()?.Metadata.GetMetadata<EnableRequestBufferingAttribute>();
		if (attribute != null) ctx.Request.EnableBuffering(attribute.MaxLength);
		await next(ctx);
	}
}

public class EnableRequestBufferingAttribute(long maxLength) : Attribute {
	internal long MaxLength = maxLength;
}