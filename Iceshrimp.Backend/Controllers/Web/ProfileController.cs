using System.Net;
using System.Net.Mime;
using Iceshrimp.Backend.Controllers.Shared.Attributes;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Backend.Core.Services;
using Iceshrimp.Shared.Schemas.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Controllers.Web;

[ApiController]
[Authenticate]
[Authorize]
[EnableRateLimiting("sliding")]
[Route("/api/iceshrimp/profile")]
[Produces(MediaTypeNames.Application.Json)]
public class ProfileController(
	IOptions<Config.InstanceSection> instance,
	UserService userSvc,
	DriveService driveSvc
) : ControllerBase
{
	[HttpGet]
	[ProducesResults(HttpStatusCode.OK)]
	public UserProfileEntity GetProfile()
	{
		var user         = HttpContext.GetUserOrFail();
		var profile      = user.UserProfile ?? throw new Exception("Local user must have profile");
		var fields       = profile.Fields.Select(p => new UserProfileEntity.Field { Name = p.Name, Value = p.Value });
		var ffVisibility = (UserProfileEntity.FFVisibilityEnum)profile.FFVisibility;

		return new UserProfileEntity
		{
			Description  = profile.Description,
			Location     = profile.Location,
			Birthday     = profile.Birthday,
			FFVisibility = ffVisibility,
			Fields       = fields.ToList(),
			DisplayName  = user.DisplayName ?? "",
			IsBot        = user.IsBot,
			IsCat        = user.IsCat,
			SpeakAsCat   = user.SpeakAsCat
		};
	}

	[HttpPut]
	[Consumes(MediaTypeNames.Application.Json)]
	[ProducesResults(HttpStatusCode.OK)]
	public async Task UpdateProfile(UserProfileEntity newProfile)
	{
		var     user     = HttpContext.GetUserOrFail();
		var     profile  = user.UserProfile ?? throw new Exception("Local user must have profile");
		string? birthday = null;

		if (!string.IsNullOrWhiteSpace(newProfile.Birthday))
		{
			if (!DateOnly.TryParseExact(newProfile.Birthday, "O", out var parsed))
				throw GracefulException.BadRequest("Invalid birthday. Expected format is: YYYY-MM-DD");
			birthday = parsed.ToString("O");
		}

		var fields = newProfile.Fields.Select(p => new UserProfile.Field
		{
			Name       = p.Name,
			Value      = p.Value,
			IsVerified = false
		});

		profile.Description  = string.IsNullOrWhiteSpace(newProfile.Description) ? null : newProfile.Description;
		profile.Location     = string.IsNullOrWhiteSpace(newProfile.Location) ? null : newProfile.Location;
		profile.Birthday     = birthday;
		profile.Fields       = fields.ToArray();
		profile.FFVisibility = (UserProfile.UserProfileFFVisibility)newProfile.FFVisibility;

		user.DisplayName = string.IsNullOrWhiteSpace(newProfile.DisplayName) ? null : newProfile.DisplayName.Trim();

		user.IsBot      = newProfile.IsBot;
		user.IsCat      = newProfile.IsCat;
		user.SpeakAsCat = newProfile is { SpeakAsCat: true, IsCat: true };

		var prevAvatarId = user.AvatarId;
		var prevBannerId = user.BannerId;
		await userSvc.UpdateLocalUserAsync(user, prevAvatarId, prevBannerId);
	}

	[HttpGet("avatar")]
	[ProducesResults(HttpStatusCode.OK)]
	public string GetAvatarUrl()
	{
		var user = HttpContext.GetUserOrFail();
		return user.AvatarId != null ? user.GetAvatarUrl(instance.Value) : "";
	}

	[HttpPost("avatar")]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.BadRequest)]
	public async Task UpdateAvatar(IFormFile file)
	{
		var user = HttpContext.GetUserOrFail();

		var prevAvatarId = user.AvatarId;
		var prevBannerId = user.BannerId;

		if (!file.ContentType.StartsWith("image/"))
			throw GracefulException.BadRequest("Avatar must be an image");

		var rq = new DriveFileCreationRequest
		{
			Filename    = file.FileName,
			IsSensitive = false,
			MimeType    = file.ContentType
		};

		var avatar = await driveSvc.StoreFileAsync(file.OpenReadStream(), user, rq);

		user.Avatar         = avatar;
		user.AvatarBlurhash = avatar.Blurhash;

		await userSvc.UpdateLocalUserAsync(user, prevAvatarId, prevBannerId);
	}

	[HttpDelete("avatar")]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.NotFound)]
	public async Task DeleteAvatar()
	{
		var user = HttpContext.GetUserOrFail();

		var prevAvatarId = user.AvatarId;
		var prevBannerId = user.BannerId;

		if (prevAvatarId == null)
			throw GracefulException.NotFound("You do not have an avatar");

		user.Avatar         = null;
		user.AvatarBlurhash = null;

		await userSvc.UpdateLocalUserAsync(user, prevAvatarId, prevBannerId);
	}

	[HttpGet("banner")]
	[ProducesResults(HttpStatusCode.OK)]
	public string GetBannerUrl()
	{
		var user = HttpContext.GetUserOrFail();
		return user.GetBannerUrl(instance.Value) ?? "";
	}

	[HttpPost("banner")]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.BadRequest)]
	public async Task UpdateBanner(IFormFile file)
	{
		var user = HttpContext.GetUserOrFail();

		var prevAvatarId = user.AvatarId;
		var prevBannerId = user.BannerId;

		if (!file.ContentType.StartsWith("image/"))
			throw GracefulException.BadRequest("Banner must be an image");

		var rq = new DriveFileCreationRequest
		{
			Filename    = file.FileName,
			IsSensitive = false,
			MimeType    = file.ContentType
		};

		var banner = await driveSvc.StoreFileAsync(file.OpenReadStream(), user, rq);

		user.Banner         = banner;
		user.BannerBlurhash = banner.Blurhash;

		await userSvc.UpdateLocalUserAsync(user, prevAvatarId, prevBannerId);
	}

	[HttpDelete("banner")]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.NotFound)]
	public async Task DeleteBanner()
	{
		var user = HttpContext.GetUserOrFail();

		var prevAvatarId = user.AvatarId;
		var prevBannerId = user.BannerId;

		if (prevBannerId == null)
			throw GracefulException.NotFound("You do not have a banner");

		user.Banner         = null;
		user.BannerBlurhash = null;

		await userSvc.UpdateLocalUserAsync(user, prevAvatarId, prevBannerId);
	}
}
