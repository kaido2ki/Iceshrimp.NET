using Microsoft.AspNetCore.Http.Features;

namespace Iceshrimp.Backend.Core.Middleware;

public class RequestBufferingMiddleware(RequestDelegate next) {
	public async Task InvokeAsync(HttpContext context) {
		var endpoint  = context.Features.Get<IEndpointFeature>()?.Endpoint;
		var attribute = endpoint?.Metadata.GetMetadata<EnableRequestBufferingAttribute>();

		if (attribute != null) context.Request.EnableBuffering(attribute.MaxLength);

		await next(context);
	}
}

public class EnableRequestBufferingAttribute(long maxLength) : Attribute {
	internal long MaxLength = maxLength;
}