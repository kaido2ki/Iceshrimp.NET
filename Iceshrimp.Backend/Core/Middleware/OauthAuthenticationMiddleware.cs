using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Helpers;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Middleware;

public class OauthAuthenticationMiddleware(DatabaseContext db) : IMiddleware {
	public async Task InvokeAsync(HttpContext ctx, RequestDelegate next) {
		var endpoint  = ctx.Features.Get<IEndpointFeature>()?.Endpoint;
		var attribute = endpoint?.Metadata.GetMetadata<AuthenticateOauthAttribute>();

		if (attribute != null) {
			var request = ctx.Request;
			var header  = request.Headers.Authorization.ToString();
			if (!header.ToLowerInvariant().StartsWith("bearer ")) {
				await next(ctx);
				return;
			}

			header = header[7..];
			var token = await db.OauthTokens
			                    .Include(p => p.User)
			                    .Include(p => p.App)
			                    .FirstOrDefaultAsync(p => p.Token == header && p.Active);

			if (token == null) {
				await next(ctx);
				return;
			}

			if (attribute.Scopes.Length > 0 &&
			    attribute.Scopes.Except(MastodonOauthHelpers.ExpandScopes(token.Scopes)).Any()) {
				await next(ctx);
				return;
			}

			ctx.SetOauthToken(token);
		}

		await next(ctx);
	}
}

public class AuthenticateOauthAttribute(params string[] scopes) : Attribute {
	public string[] Scopes = scopes;
}

public static partial class HttpContextExtensions {
	private const string MastodonKey = "masto-session";

	internal static void SetOauthToken(this HttpContext ctx, OauthToken session) {
		ctx.Items.Add(MastodonKey, session);
	}

	public static OauthToken? GetOauthToken(this HttpContext ctx) {
		ctx.Items.TryGetValue(MastodonKey, out var session);
		return session as OauthToken;
	}

	public static User? GetOauthUser(this HttpContext ctx) {
		ctx.Items.TryGetValue(MastodonKey, out var session);
		return (session as OauthToken)?.User;
	}
}