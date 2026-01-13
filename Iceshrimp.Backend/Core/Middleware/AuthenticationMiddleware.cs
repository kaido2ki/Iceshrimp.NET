using Iceshrimp.Backend.Controllers.Mastodon.Attributes;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Helpers;
using Iceshrimp.Backend.Core.Services;
using Iceshrimp.Utils.DependencyInjection;
using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Middleware;

public class AuthenticationMiddleware(
	DatabaseContext db,
	UserService userSvc,
	FlagService flags
) : ConditionalMiddleware<AuthenticateAttribute>, IMiddlewareService
{
	public static ServiceLifetime Lifetime => ServiceLifetime.Scoped;

	public async Task InvokeAsync(HttpContext ctx, RequestDelegate next)
	{
		var endpoint  = ctx.GetEndpoint();
		var attribute = GetAttributeOrFail(ctx);

		var isBlazorSsr = endpoint?.Metadata.GetMetadata<RootComponentMetadata>() != null;
		if (isBlazorSsr)
		{
			await AuthenticateBlazorSsrAsync(ctx, attribute);
			await next(ctx);
			return;
		}

		ctx.Response.Headers.CacheControl = "private, no-store";
		var request = ctx.Request;
		var header  = request.Headers.Authorization.ToString();
		if (!header.ToLowerInvariant().StartsWith("bearer "))
		{
			await next(ctx);
			return;
		}

		var token = header[7..];

		var isMastodon = endpoint?.Metadata.GetMetadata<MastodonApiControllerAttribute>() != null;
		if (isMastodon)
		{
			var oauthToken = await db.OauthTokens
			                         .Include(p => p.User.UserProfile)
			                         .Include(p => p.User.UserSettings)
			                         .Include(p => p.App)
			                         .FirstOrDefaultAsync(p => p.Token == token && p.Active);

			if (oauthToken?.User.IsSuspended == true)
				throw GracefulException
					.Unauthorized("Your access has been suspended by the instance administrator.");

			if (oauthToken == null)
			{
				await next(ctx);
				return;
			}

			if ((attribute.AdminRole && !oauthToken.User.IsAdmin) ||
			    (attribute.ModeratorRole && oauthToken.User is { IsAdmin: false, IsModerator: false }) ||
			    (attribute.Scopes.Any(p => p is "admin" || p.StartsWith("admin:")) && !oauthToken.User.IsAdmin))
			{
				await next(ctx);
				return;
			}

			if (attribute.Scopes.Length > 0 &&
			    attribute.Scopes.Except(MastodonOauthHelpers.ExpandScopes(oauthToken.Scopes)).Any())
			{
				await next(ctx);
				return;
			}

			userSvc.UpdateOauthTokenMetadata(oauthToken);
			ctx.SetOauthToken(oauthToken);

			flags.SupportsHtmlFormatting.Value = oauthToken.SupportsHtmlFormatting;
			flags.SupportsInlineMedia.Value    = oauthToken.SupportsInlineMedia;
			flags.IsPleroma.Value              = oauthToken.IsPleroma;
		}
		else
		{
			var session = await db.Sessions
			                      .Include(p => p.User.UserProfile)
			                      .Include(p => p.User.UserSettings)
			                      .FirstOrDefaultAsync(p => p.Token == token);

			if (session is { Active: false } && !attribute.AllowInactive)
				session = null;

			if (session?.User.IsSuspended == true)
				throw GracefulException
					.Unauthorized("Your access has been suspended by the instance administrator.");

			if (session == null)
			{
				await next(ctx);
				return;
			}

			if ((attribute.AdminRole && !session.User.IsAdmin) ||
			    (attribute.ModeratorRole &&
			     session.User is { IsAdmin: false, IsModerator: false }))
			{
				await next(ctx);
				return;
			}

			userSvc.UpdateSessionMetadata(session);
			ctx.SetSession(session);
		}

		await next(ctx);
	}

	private async Task AuthenticateBlazorSsrAsync(HttpContext ctx, AuthenticateAttribute attribute)
	{
		if (!ctx.Request.Cookies.TryGetValue("admin_session", out var token)) return;

		var session = await db.Sessions
		                      .Include(p => p.User.UserProfile)
		                      .Include(p => p.User.UserSettings)
		                      .FirstOrDefaultAsync(p => p.Token == token && p.Active);

		if (session == null)
			return;
		if (session.User.IsSuspended)
			throw GracefulException.Unauthorized("Your access has been suspended by the instance administrator.");

		if (
			(attribute.AdminRole && !session.User.IsAdmin) ||
			(attribute.ModeratorRole && session.User is { IsAdmin: false, IsModerator: false })
		)
		{
			return;
		}

		userSvc.UpdateSessionMetadata(session);
		ctx.SetSession(session);
	}
}

public class AuthenticateAttribute(params string[] scopes) : Attribute
{
	public readonly bool     AdminRole     = scopes.Contains("role:admin");
	public readonly bool     ModeratorRole = scopes.Contains("role:moderator");
	public readonly string[] Scopes        = scopes.Where(p => !p.StartsWith("role:")).ToArray();

	public bool AllowInactive { get; init; } = false;
}

public static partial class HttpContextExtensions
{
	private const string Key           = "session";
	private const string MastodonKey   = "masto-session";

	extension(HttpContext ctx)
	{
		internal void SetSession(Session session)
		{
			ctx.Items.Add(Key, session);
		}

		public Session? GetSession()
		{
			ctx.Items.TryGetValue(Key, out var session);
			return session as Session;
		}

		public Session GetSessionOrFail()
		{
			return ctx.GetSession() ?? throw new Exception("Failed to get session from HttpContext");
		}

		internal void SetOauthToken(OauthToken session)
		{
			ctx.Items.Add(MastodonKey, session);
		}

		public OauthToken? GetOauthToken()
		{
			ctx.Items.TryGetValue(MastodonKey, out var session);
			return session as OauthToken;
		}

		//TODO: Is it faster to check for the MastodonApiControllerAttribute here?
		public User? GetUser()
		{
			if (ctx.Items.TryGetValue(Key, out var session))
				return (session as Session)?.User;
			return ctx.Items.TryGetValue(MastodonKey, out var token)
				? (token as OauthToken)?.User
				: null;
		}

		public User GetUserOrFail()
		{
			return ctx.GetUser() ?? throw new Exception("Failed to get user from HttpContext");
		}
	}
}