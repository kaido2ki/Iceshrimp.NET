using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Mime;
using Iceshrimp.Backend.Controllers.Shared.Attributes;
using Iceshrimp.Backend.Controllers.Web.Renderers;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Helpers;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Backend.Core.Services;
using Iceshrimp.Shared.Schemas.Web;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Controllers.Web;

/// <summary>
/// <para>Operations for user authentication. Iceshrimp.NET's API uses Bearer tokens in the <c>Authorization</c> header for authentication.</para>
/// <para>The CORS rules for these operations are restricted to only allow domains set in <c>[Instance] AdditionalDomains</c>. Web based clients that aren't set will not be able to use these operations.</para>
/// </summary>
[ApiController]
[Tags("Authentication")]
[EnableRateLimiting("sliding")]
[Produces(MediaTypeNames.Application.Json)]
[Route("/api/iceshrimp/auth")]
[EnableCors("iceshrimp-trusted")]
public class AuthController(DatabaseContext db, UserService userSvc, UserRenderer userRenderer) : ControllerBase
{
	/// <summary>
	/// Get authentication status
	/// </summary>
	/// <remarks>Returns the authentication status for the current session (token).</remarks>
	/// <response code="200">Current authentication status</response>
	[HttpGet]
	[Authenticate(AllowInactive = true)]
	[ProducesResults(HttpStatusCode.OK)]
	[EnableCors("iceshrimp")]
	public async Task<AuthResponse> GetAuthStatus()
	{
		var session = HttpContext.GetSession();
		if (session == null) return new AuthResponse { Status = AuthStatusEnum.Guest };

		return await GetAuthResponse(session, session.User);
	}

	/// <summary>
	/// Log in
	/// </summary>
	/// <remarks>Log in and request a new token.</remarks>
	/// <param name="request">Authentication request</param>
	/// <response code="200">New authentication status</response>
	/// <response code="403">Invalid username or password</response>
	[HttpPost("login")]
	[HideRequestDuration]
	[EnableRateLimiting("auth")]
	[Consumes(MediaTypeNames.Application.Json)]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.BadRequest, HttpStatusCode.Forbidden)]
	[SuppressMessage("ReSharper.DPA", "DPA0011: High execution time of MVC action",
	                 Justification = "Argon2 is execution time-heavy by design")]
	public async Task<AuthResponse> Login([FromBody] AuthRequest request)
	{
		var user = await db.Users.FirstOrDefaultAsync(p => p.IsLocalUser
		                                                   && p.UsernameLower == request.Username.ToLowerInvariant());
		if (user == null)
			throw GracefulException.Forbidden("Invalid username or password");
		if (user.IsSystemUser)
			throw GracefulException.BadRequest("Cannot log in as system user");
		var settings = await db.UserSettings.FirstOrDefaultAsync(p => p.User == user);
		if (settings?.Password == null)
			throw GracefulException.Forbidden("Invalid username or password");
		if (!AuthHelpers.ComparePassword(request.Password, settings.Password))
			throw GracefulException.Forbidden("Invalid username or password");

		var session = HttpContext.GetSession();
		if (session == null)
		{
			session = new Session
			{
				Id        = IdHelpers.GenerateSnowflakeId(),
				UserId    = user.Id,
				Active    = !settings.TwoFactorEnabled,
				CreatedAt = DateTime.UtcNow,
				Token     = CryptographyHelpers.GenerateRandomString(32)
			};
			await db.AddAsync(session);
			await db.SaveChangesAsync();
		}

		return await GetAuthResponse(session, user);
	}

	/// <summary>
	/// Register account
	/// </summary>
	/// <remarks>Register a new account and request a token.</remarks>
	/// <param name="request">Registration request</param>
	/// <response code="200">New authentication status</response>
	[HttpPost("register")]
	[EnableRateLimiting("auth")]
	[Consumes(MediaTypeNames.Application.Json)]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.BadRequest, HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden)]
	public async Task<AuthResponse> Register([FromBody] RegistrationRequest request)
	{
		//TODO: captcha support

		await userSvc.CreateLocalUserAsync(request.Username, request.Password, request.Invite);
		return await Login(request);
	}

	/// <summary>
	/// Two-factor authentication
	/// </summary>
	/// <remarks>Authenticate a session that has its <c>status</c> as <c>two_factor</c>.</remarks>
	/// <param name="request">Two-factor authentication request</param>
	/// <response code="200">New authentication status</response>
	/// <response code="400">Two-factor authentication is disabled</response>
	/// <response code="403">Two-factor authentication code is invalid</response>
	[HttpPost("2fa")]
	[Authenticate(AllowInactive = true)]
	[Authorize(AllowInactive = true)]
	[EnableRateLimiting("auth")]
	[Consumes(MediaTypeNames.Application.Json)]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.BadRequest, HttpStatusCode.Forbidden)]
	public async Task<AuthResponse> SubmitTwoFactor([FromBody] TwoFactorRequest request)
	{
		var user    = HttpContext.GetUserOrFail();
		var session = HttpContext.GetSessionOrFail();

		if (session.Active)
			return await GetAuthResponse(session, user);
		if (user.UserSettings?.TwoFactorEnabled != true)
			throw GracefulException.BadRequest("2FA is disabled");
		if (user.UserSettings?.TwoFactorSecret is not { } secret)
			throw new Exception("2FA is enabled but no secret is known");
		if (request.Code is not { Length: 6 } totp)
			throw GracefulException.Forbidden("Missing or invalid TOTP code");
		if (!TotpHelper.Validate(secret, totp))
			throw GracefulException.Forbidden("Invalid TOTP code");

		session.Active = true;
		await db.SaveChangesAsync();
		return await GetAuthResponse(session, user);
	}

	/// <summary>
	/// Log out
	/// </summary>
	/// <response code="200">Logged out</response>
	[HttpPost("logout")]
	[Authenticate]
	[Authorize]
	[ProducesResults(HttpStatusCode.OK)]
	public async Task Logout()
	{
		var session = HttpContext.GetSessionOrFail();
		db.Remove(session);
		await db.SaveChangesAsync();
	}

	/// <summary>
	/// Change password
	/// </summary>
	/// <param name="request">Change password request</param>
	/// <response code="200">New authentication status</response>
	[HttpPost("change-password")]
	[Authenticate]
	[Authorize]
	[EnableRateLimiting("auth")]
	[Consumes(MediaTypeNames.Application.Json)]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.BadRequest)]
	[SuppressMessage("ReSharper.DPA", "DPA0011: High execution time of MVC action",
	                 Justification = "Argon2 is execution time-heavy by design")]
	public async Task<AuthResponse> ChangePassword([FromBody] ChangePasswordRequest request)
	{
		var user     = HttpContext.GetUserOrFail();
		var settings = await db.UserSettings.FirstOrDefaultAsync(p => p.User == user);
		if (settings is not { Password: not null }) throw new Exception("settings?.Password was null");
		if (!AuthHelpers.ComparePassword(request.OldPassword, settings.Password))
			throw GracefulException.BadRequest("Old password is incorrect");
		if (request.NewPassword.Length < 8)
			throw GracefulException.BadRequest("Password must be at least 8 characters long");

		settings.Password = AuthHelpers.HashPassword(request.NewPassword);
		await db.SaveChangesAsync();

		return await Login(new AuthRequest { Username = user.Username, Password = request.NewPassword });
	}

	private async Task<AuthResponse> GetAuthResponse(Session session, User user)
	{
		return new AuthResponse
		{
			Status      = session.Active ? AuthStatusEnum.Authenticated : AuthStatusEnum.TwoFactor,
			Token       = session.Token,
			IsAdmin     = session.User.IsAdmin,
			IsModerator = session.User.IsModerator,
			User        = await userRenderer.RenderOne(user)
		};
	}
}
