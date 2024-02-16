using Iceshrimp.Backend.Controllers.Mastodon.Attributes;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Middleware;

public class AuthenticationMiddleware(DatabaseContext db) : IMiddleware {
	public async Task InvokeAsync(HttpContext ctx, RequestDelegate next) {
		var endpoint  = ctx.GetEndpoint();
		var attribute = endpoint?.Metadata.GetMetadata<AuthenticateAttribute>();

		if (attribute != null) {
			var request = ctx.Request;
			var header  = request.Headers.Authorization.ToString();
			if (!header.ToLowerInvariant().StartsWith("bearer ")) {
				await next(ctx);
				return;
			}

			var token = header[7..];

			var isMastodon = endpoint?.Metadata.GetMetadata<MastodonApiControllerAttribute>() != null;
			if (isMastodon) {
				var oauthToken = await db.OauthTokens
				                         .Include(p => p.User)
				                         .ThenInclude(p => p.UserProfile)
				                         .Include(p => p.App)
				                         .FirstOrDefaultAsync(p => p.Token == token && p.Active);

				if (oauthToken == null) {
					await next(ctx);
					return;
				}

				if (attribute.AdminRole && !oauthToken.User.IsAdmin || attribute.ModeratorRole &&
				    oauthToken.User is { IsAdmin: false, IsModerator: false }) {
					await next(ctx);
					return;
				}

				if (attribute.Scopes.Length > 0 &&
				    attribute.Scopes.Except(MastodonOauthHelpers.ExpandScopes(oauthToken.Scopes)).Any()) {
					await next(ctx);
					return;
				}

				ctx.SetOauthToken(oauthToken);
			}
			else {
				var session = await db.Sessions
				                      .Include(p => p.User)
				                      .ThenInclude(p => p.UserProfile)
				                      .FirstOrDefaultAsync(p => p.Token == token && p.Active);

				if (session == null) {
					await next(ctx);
					return;
				}

				if (attribute.AdminRole && !session.User.IsAdmin || attribute.ModeratorRole &&
				    session.User is { IsAdmin: false, IsModerator: false }) {
					await next(ctx);
					return;
				}

				ctx.SetSession(session);
			}
		}

		await next(ctx);
	}
}

public class AuthenticateAttribute(params string[] scopes) : Attribute {
	public readonly string[] Scopes        = scopes.Where(p => !p.StartsWith("role:")).ToArray();
	public readonly bool     AdminRole     = scopes.Contains("role:admin");
	public readonly bool     ModeratorRole = scopes.Contains("role:moderator");
}

public static partial class HttpContextExtensions {
	private const string Key         = "session";
	private const string MastodonKey = "masto-session";

	internal static void SetSession(this HttpContext ctx, Session session) {
		ctx.Items.Add(Key, session);
	}

	public static Session? GetSession(this HttpContext ctx) {
		ctx.Items.TryGetValue(Key, out var session);
		return session as Session;
	}

	internal static void SetOauthToken(this HttpContext ctx, OauthToken session) {
		ctx.Items.Add(MastodonKey, session);
	}

	public static OauthToken? GetOauthToken(this HttpContext ctx) {
		ctx.Items.TryGetValue(MastodonKey, out var session);
		return session as OauthToken;
	}

	//TODO: Is it faster to check for the MastodonApiControllerAttribute here?
	public static User? GetUser(this HttpContext ctx) {
		if (ctx.Items.TryGetValue(Key, out var session))
			return (session as Session)?.User;
		return ctx.Items.TryGetValue(MastodonKey, out var token)
			? (token as OauthToken)?.User
			: null;
	}

	public static User GetUserOrFail(this HttpContext ctx) {
		return ctx.GetUser() ?? throw new GracefulException("Failed to get user from HttpContext");
	}
}