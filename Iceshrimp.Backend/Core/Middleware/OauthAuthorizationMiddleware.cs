using Microsoft.AspNetCore.Http.Features;

namespace Iceshrimp.Backend.Core.Middleware;

public class OauthAuthorizationMiddleware : IMiddleware {
	public async Task InvokeAsync(HttpContext ctx, RequestDelegate next) {
		var endpoint  = ctx.Features.Get<IEndpointFeature>()?.Endpoint;
		var attribute = endpoint?.Metadata.GetMetadata<AuthorizeOauthAttribute>();

		if (attribute != null)
			if (ctx.GetOauthToken() is not { Active: true })
				throw GracefulException.Forbidden("This method requires an authenticated user");

		await next(ctx);
	}
}

public class AuthorizeOauthAttribute : Attribute;
