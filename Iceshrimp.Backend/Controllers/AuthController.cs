using System.Net.Mime;
using Iceshrimp.Backend.Controllers.Schemas;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Helpers;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Backend.Core.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Controllers;

[ApiController]
[Tags("Authentication")]
[EnableRateLimiting("sliding")]
[Produces("application/json")]
[Route("/api/iceshrimp/v1/auth")]
public class AuthController(DatabaseContext db, UserService userSvc) : Controller {
	[HttpGet]
	[Authenticate]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AuthResponse))]
	public IActionResult GetAuthStatus() {
		var session = Request.HttpContext.GetSession();

		if (session == null)
			return Ok(new AuthResponse {
				Status = AuthStatusEnum.Guest
			});

		return Ok(new AuthResponse {
			Status = session.Active ? AuthStatusEnum.Authenticated : AuthStatusEnum.TwoFactor,
			Token  = session.Token,
			User = new UserResponse {
				Username  = session.User.Username,
				Id        = session.User.Id,
				AvatarUrl = session.User.AvatarUrl,
				BannerUrl = session.User.BannerUrl
			}
		});
	}

	[HttpPost]
	[EnableRateLimiting("strict")]
	[Consumes(MediaTypeNames.Application.Json)]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TimelineResponse))]
	[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
	[ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ErrorResponse))]
	public async Task<IActionResult> Login([FromBody] AuthRequest request) {
		var user = await db.Users.FirstOrDefaultAsync(p => p.Host == null &&
		                                                   p.UsernameLower == request.Username.ToLowerInvariant());
		if (user == null)
			return StatusCode(StatusCodes.Status403Forbidden);
		var profile = await db.UserProfiles.FirstOrDefaultAsync(p => p.User == user);
		if (profile?.Password == null)
			return StatusCode(StatusCodes.Status403Forbidden);
		if (!AuthHelpers.ComparePassword(request.Password, profile.Password))
			return StatusCode(StatusCodes.Status403Forbidden);

		var res = await db.AddAsync(new Session {
			Id        = IdHelpers.GenerateSlowflakeId(),
			UserId    = user.Id,
			Active    = !profile.TwoFactorEnabled,
			CreatedAt = new DateTime(),
			Token     = CryptographyHelpers.GenerateRandomString(32)
		});

		var session = res.Entity;
		await db.AddAsync(session);
		await db.SaveChangesAsync();

		return Ok(new AuthResponse {
			Status = session.Active ? AuthStatusEnum.Authenticated : AuthStatusEnum.TwoFactor,
			Token  = session.Token,
			User = new UserResponse {
				Username  = session.User.Username,
				Id        = session.User.Id,
				AvatarUrl = session.User.AvatarUrl,
				BannerUrl = session.User.BannerUrl
			}
		});
	}

	[HttpPut]
	[EnableRateLimiting("strict")]
	[Consumes(MediaTypeNames.Application.Json)]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TimelineResponse))]
	[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
	[ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ErrorResponse))]
	[ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ErrorResponse))]
	public async Task<IActionResult> Register([FromBody] RegistrationRequest request) {
		//TODO: captcha support

		await userSvc.CreateLocalUser(request.Username, request.Password, request.Invite);
		return await Login(request);
	}

	//TODO: PATCH = update password
}