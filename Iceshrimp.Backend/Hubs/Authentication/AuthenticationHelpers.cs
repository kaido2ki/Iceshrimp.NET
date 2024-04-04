using System.Security.Claims;
using System.Text.Encodings.Web;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Backend.Core.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Hubs.Authentication;

public class HubAuthorizationRequirement : IAuthorizationRequirement;

public class HubAuthenticationHandler(
	IOptionsMonitor<BearerTokenOptions> options,
	ILoggerFactory logger,
	UrlEncoder encoder,
	DatabaseContext db,
	UserService userSvc
) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
	protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
	{
		string token;
		if (Request.Query.ContainsKey("access_token"))
		{
			token = Request.Query["access_token"].ToString();
		}
		else
		{
			var header = Request.Headers.Authorization.ToString();
			if (!header.ToLowerInvariant().StartsWith("bearer "))
				return AuthenticateResult.NoResult();

			token = header[7..];
		}

		var session = await db.Sessions
		                      .Include(p => p.User.UserProfile)
		                      .Include(p => p.User.UserSettings)
		                      .FirstOrDefaultAsync(p => p.Token == token && p.Active);

		if (session is not { Active: true })
			return AuthenticateResult.NoResult();

		var claims   = new[] { new Claim("token", token), new Claim("userId", session.UserId) };
		var identity = new ClaimsIdentity(claims, nameof(HubAuthenticationHandler));
		var ticket   = new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name);
		userSvc.UpdateSessionMetadata(session);
		Context.SetSession(session);
		return AuthenticateResult.Success(ticket);
	}
}

public class HubAuthorizationHandler(
	IHttpContextAccessor httpContextAccessor
) : AuthorizationHandler<HubAuthorizationRequirement>
{
	protected override Task HandleRequirementAsync(
		AuthorizationHandlerContext context, HubAuthorizationRequirement requirement
	)
	{
		var ctx = httpContextAccessor.HttpContext;
		if (ctx == null)
			throw new Exception("HttpContext must not be null at this stage");

		if (ctx.GetUser() == null)
			context.Fail(new AuthorizationFailureReason(this, "Unauthorized"));
		else
			context.Succeed(requirement);

		return Task.CompletedTask;
	}
}

public class HubUserIdProvider(IHttpContextAccessor httpContextAccessor) : IUserIdProvider
{
	public string? GetUserId(HubConnectionContext connection)
	{
		if (httpContextAccessor.HttpContext == null)
			throw new Exception("HttpContext must not be null at this stage");

		return httpContextAccessor.HttpContext.GetUser()?.Id;
	}
}

public static class AuthenticationServiceExtensions
{
	public static void AddAuthenticationServices(this IServiceCollection services)
	{
		services.AddScoped<IAuthenticationHandler, HubAuthenticationHandler>()
		        .AddSingleton<IAuthorizationHandler, HubAuthorizationHandler>()
		        .AddSingleton<IHttpContextAccessor, HttpContextAccessor>()
		        .AddSingleton<IUserIdProvider, HubUserIdProvider>();
	}
}