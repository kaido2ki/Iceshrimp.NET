using System.Net;
using System.Net.Mime;
using Iceshrimp.Backend.Controllers.Schemas;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Helpers;
using Iceshrimp.Backend.Core.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Controllers;

[ApiController]
[Produces("application/json")]
[Route("/api/iceshrimp/v1/auth")]
public class AuthController(DatabaseContext db, UserService userSvc) : Controller {
	[HttpGet]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AuthResponse))]
	public IActionResult GetAuthStatus() {
		return new StatusCodeResult((int)HttpStatusCode.NotImplemented);
	}

	[HttpPost]
	[Consumes(MediaTypeNames.Application.Json)]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TimelineResponse))]
	[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
	[ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ErrorResponse))]
	public async Task<IActionResult> Login([FromBody] AuthRequest request) {
		var user = await db.Users.FirstOrDefaultAsync(p => p.Host == null &&
		                                                   p.UsernameLower == request.Username.ToLowerInvariant());
		if (user == null) return Unauthorized();
		var profile = await db.UserProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);
		if (profile?.Password == null) return Unauthorized();
		if (!AuthHelpers.ComparePassword(request.Password, profile.Password)) return Unauthorized();

		var res = await db.AddAsync(new Session {
			Id        = IdHelpers.GenerateSlowflakeId(),
			UserId    = user.Id,
			Active    = !profile.TwoFactorEnabled,
			CreatedAt = new DateTime(),
			Token     = CryptographyHelpers.GenerateRandomString(32)
		});

		var session = res.Entity;

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
	[Consumes(MediaTypeNames.Application.Json)]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TimelineResponse))]
	[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
	[ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ErrorResponse))]
	public async Task<IActionResult> Register([FromBody] AuthRequest request) {
		//TODO: captcha support
		//TODO: invite support

		await userSvc.CreateLocalUser(request.Username, request.Password);
		return await Login(request);
	}

	//TODO: PATCH = update password
}