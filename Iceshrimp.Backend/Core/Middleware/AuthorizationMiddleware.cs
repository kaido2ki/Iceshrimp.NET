using Iceshrimp.Backend.Controllers.Mastodon.Attributes;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Helpers;

namespace Iceshrimp.Backend.Core.Middleware;

public class AuthorizationMiddleware(RequestDelegate next) : ConditionalMiddleware<AuthorizeAttribute>
{
	public async Task InvokeAsync(HttpContext ctx)
	{
		var endpoint  = ctx.GetEndpoint();
		var attribute = GetAttributeOrFail(ctx);

		ctx.Response.Headers.CacheControl = "private, no-store";
		var isMastodon = endpoint?.Metadata.GetMetadata<MastodonApiControllerAttribute>() != null;

		if (isMastodon)
		{
			var token = ctx.GetOauthToken();
			if (token is not { Active: true })
				throw GracefulException.Unauthorized("This method requires an authenticated user");
			if (attribute.Scopes.Length > 0 &&
			    attribute.Scopes.Except(MastodonOauthHelpers.ExpandScopes(token.Scopes)).Any())
				throw GracefulException.Forbidden("This action is outside the authorized scopes");
			if (attribute.AdminRole && !token.User.IsAdmin)
				throw GracefulException.Forbidden("This action is outside the authorized scopes");
			if (attribute.ModeratorRole && token.User is { IsAdmin: false, IsModerator: false })
				throw GracefulException.Forbidden("This action is outside the authorized scopes");
			if (attribute.Scopes.Any(p => p is "admin" || p.StartsWith("admin:") && !token.User.IsAdmin))
				throw GracefulException.Forbidden("This action is outside the authorized scopes");
		}
		else
		{
			var session = ctx.GetSession();
			if (session == null || (!session.Active && !attribute.AllowInactive))
				throw GracefulException.Unauthorized("This method requires an authenticated user");
			if (attribute.AdminRole && !session.User.IsAdmin)
				throw GracefulException.Forbidden("This action is outside the authorized scopes");
			if (attribute.ModeratorRole && session.User is { IsAdmin: false, IsModerator: false })
				throw GracefulException.Forbidden("This action is outside the authorized scopes");
		}

		await next(ctx);
	}
}

public class AuthorizeAttribute(params string[] scopes) : Attribute
{
	public readonly bool     AdminRole     = scopes.Contains("role:admin");
	public readonly bool     ModeratorRole = scopes.Contains("role:moderator");
	public readonly string[] Scopes        = scopes.Where(p => !p.StartsWith("role:")).ToArray();

	public bool AllowInactive { get; init; } = false;
}