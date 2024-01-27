using Microsoft.AspNetCore.Http.Features;

namespace Iceshrimp.Backend.Core.Middleware;

public class AuthorizationMiddleware : IMiddleware {
	public async Task InvokeAsync(HttpContext ctx, RequestDelegate next) {
		var endpoint  = ctx.Features.Get<IEndpointFeature>()?.Endpoint;
		var attribute = endpoint?.Metadata.GetMetadata<AuthorizeAttribute>();

		if (attribute != null)
			if (ctx.GetSession() is not { Active: true })
				throw GracefulException.Forbidden("This method requires an authenticated user");

		await next(ctx);
	}
}

public class AuthorizeAttribute : Attribute;