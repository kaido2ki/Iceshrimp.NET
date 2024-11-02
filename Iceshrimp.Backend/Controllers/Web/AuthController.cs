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
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Controllers.Web;

[ApiController]
[Tags("Authentication")]
[EnableRateLimiting("sliding")]
[Produces(MediaTypeNames.Application.Json)]
[Route("/api/iceshrimp/auth")]
public class AuthController(DatabaseContext db, UserService userSvc, UserRenderer userRenderer) : ControllerBase
{
	[HttpGet]
	[Authenticate]
	[ProducesResults(HttpStatusCode.OK)]
	public async Task<AuthResponse> GetAuthStatus()
	{
		var session = HttpContext.GetSession();
		if (session == null) return new AuthResponse { Status = AuthStatusEnum.Guest };

		return new AuthResponse
		{
			Status      = session.Active ? AuthStatusEnum.Authenticated : AuthStatusEnum.TwoFactor,
			Token       = session.Token,
			IsAdmin     = session.User.IsAdmin,
			IsModerator = session.User.IsModerator,
			User        = await userRenderer.RenderOne(session.User)
		};
	}

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
		var user = await db.Users.FirstOrDefaultAsync(p => p.IsLocalUser &&
		                                                   p.UsernameLower == request.Username.ToLowerInvariant());
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

		return new AuthResponse
		{
			Status      = session.Active ? AuthStatusEnum.Authenticated : AuthStatusEnum.TwoFactor,
			Token       = session.Token,
			IsAdmin     = session.User.IsAdmin,
			IsModerator = session.User.IsModerator,
			User        = await userRenderer.RenderOne(user)
		};
	}

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
			throw GracefulException.BadRequest("old_password is invalid");
		if (request.NewPassword.Length < 8)
			throw GracefulException.BadRequest("Password must be at least 8 characters long");

		settings.Password = AuthHelpers.HashPassword(request.NewPassword);
		await db.SaveChangesAsync();

		return await Login(new AuthRequest { Username = user.Username, Password = request.NewPassword });
	}
}