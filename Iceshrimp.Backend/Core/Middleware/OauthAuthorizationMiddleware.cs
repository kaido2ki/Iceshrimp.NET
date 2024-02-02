using Iceshrimp.Backend.Core.Helpers;
using Microsoft.AspNetCore.Http.Features;

namespace Iceshrimp.Backend.Core.Middleware;

public class OauthAuthorizationMiddleware : IMiddleware {
	public async Task InvokeAsync(HttpContext ctx, RequestDelegate next) {
		var endpoint  = ctx.Features.Get<IEndpointFeature>()?.Endpoint;
		var attribute = endpoint?.Metadata.GetMetadata<AuthorizeOauthAttribute>();

		if (attribute != null) {
			var token = ctx.GetOauthToken();
			if (token is not { Active: true })
				throw GracefulException.Unauthorized("This method requires an authenticated user");
			if (attribute.Scopes.Length > 0 &&
			    attribute.Scopes.Except(MastodonOauthHelpers.ExpandScopes(token.Scopes)).Any())
				throw GracefulException.Forbidden("This action is outside the authorized scopes");
		}

		await next(ctx);
	}
}

public class AuthorizeOauthAttribute(params string[] scopes) : Attribute {
	public readonly string[] Scopes = scopes;
}