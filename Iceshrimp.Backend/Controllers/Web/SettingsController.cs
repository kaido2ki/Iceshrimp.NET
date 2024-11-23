using System.Net;
using System.Net.Mime;
using System.Text;
using AngleSharp.Text;
using Iceshrimp.Backend.Controllers.Shared.Attributes;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Helpers;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Backend.Core.Services;
using Iceshrimp.Shared.Schemas.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QRCoder;

namespace Iceshrimp.Backend.Controllers.Web;

[ApiController]
[Authenticate]
[Authorize]
[EnableRateLimiting("sliding")]
[Route("/api/iceshrimp/settings")]
[Produces(MediaTypeNames.Application.Json)]
public class SettingsController(
	DatabaseContext db,
	ImportExportService importExportSvc,
	MetaService meta,
	IOptions<Config.InstanceSection> instance
) : ControllerBase
{
	[HttpGet]
	[ProducesResults(HttpStatusCode.OK)]
	public async Task<UserSettingsResponse> GetSettings()
	{
		var settings = await GetOrInitUserSettings();
		return new UserSettingsResponse
		{
			FilterInaccessible      = settings.FilterInaccessible,
			PrivateMode             = settings.PrivateMode,
			AlwaysMarkSensitive     = settings.AlwaysMarkSensitive,
			AutoAcceptFollowed      = settings.AutoAcceptFollowed,
			DefaultNoteVisibility   = (NoteVisibility)settings.DefaultNoteVisibility,
			DefaultRenoteVisibility = (NoteVisibility)settings.DefaultNoteVisibility,
			TwoFactorEnrolled       = settings.TwoFactorEnabled
		};
	}

	[HttpPut]
	[Consumes(MediaTypeNames.Application.Json)]
	[ProducesResults(HttpStatusCode.OK)]
	public async Task UpdateSettings(UserSettingsRequest newSettings)
	{
		var settings = await GetOrInitUserSettings();

		settings.FilterInaccessible      = newSettings.FilterInaccessible;
		settings.PrivateMode             = newSettings.PrivateMode;
		settings.AlwaysMarkSensitive     = newSettings.AlwaysMarkSensitive;
		settings.AutoAcceptFollowed      = newSettings.AutoAcceptFollowed;
		settings.DefaultNoteVisibility   = (Note.NoteVisibility)newSettings.DefaultNoteVisibility;
		settings.DefaultRenoteVisibility = (Note.NoteVisibility)newSettings.DefaultRenoteVisibility;

		await db.SaveChangesAsync();
	}

	[HttpPost("2fa/enroll")]
	[EnableRateLimiting("auth")]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.BadRequest)]
	public async Task<TwoFactorEnrollmentResponse> EnrollTwoFactor()
	{
		var user = HttpContext.GetUserOrFail();
		if (user.UserSettings is not { } settings)
			throw new Exception("Failed to get user settings object");
		if (settings.TwoFactorEnabled)
			throw GracefulException.BadRequest("2FA is already enabled.");

		return await EnrollNewTwoFactorSecret(settings, user);
	}

	[HttpPost("2fa/reenroll")]
	[EnableRateLimiting("auth")]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.BadRequest, HttpStatusCode.Forbidden)]
	public async Task<TwoFactorEnrollmentResponse> ReenrollTwoFactor(TwoFactorRequest request)
	{
		var user = HttpContext.GetUserOrFail();
		if (user.UserSettings is not { } settings)
			throw new Exception("Failed to get user settings object");
		if (!settings.TwoFactorEnabled)
			throw GracefulException.BadRequest("2FA is not enabled.");
		if (settings.TwoFactorSecret is not { } secret)
			throw new Exception("2FA is enabled but no secret is set");
		if (!TotpHelper.Validate(secret, request.Code))
			throw GracefulException.Forbidden("Invalid TOTP");

		return await EnrollNewTwoFactorSecret(settings, user);
	}

	[HttpPost("2fa/confirm")]
	[EnableRateLimiting("auth")]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.BadRequest, HttpStatusCode.Forbidden)]
	public async Task ConfirmTwoFactor(TwoFactorRequest request)
	{
		var user = HttpContext.GetUserOrFail();
		if (user.UserSettings is not { } settings)
			throw new Exception("Failed to get user settings object");
		if (settings.TwoFactorTempSecret is not { } secret)
			throw GracefulException.BadRequest("No pending 2FA enrollment found");
		if (!TotpHelper.Validate(secret, request.Code))
			throw GracefulException.Forbidden("Invalid TOTP");

		settings.TwoFactorEnabled    = true;
		settings.TwoFactorSecret     = secret;
		settings.TwoFactorTempSecret = null;

		await db.SaveChangesAsync();
	}

	[HttpPost("2fa/disable")]
	[EnableRateLimiting("auth")]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.BadRequest, HttpStatusCode.Forbidden)]
	public async Task DisableTwoFactor(TwoFactorRequest request)
	{
		var user = HttpContext.GetUserOrFail();
		if (user.UserSettings is not { } settings)
			throw new Exception("Failed to get user settings object");
		if (!settings.TwoFactorEnabled)
			throw GracefulException.BadRequest("2FA is not enabled.");
		if (settings.TwoFactorSecret is not { } secret)
			throw new Exception("2FA is enabled but no secret is set");
		if (!TotpHelper.Validate(secret, request.Code))
			throw GracefulException.Forbidden("Invalid TOTP");

		settings.TwoFactorEnabled    = false;
		settings.TwoFactorSecret     = null;
		settings.TwoFactorTempSecret = null;

		await db.SaveChangesAsync();
	}

	[HttpPost("export/following")]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.BadRequest)]
	public async Task<FileContentResult> ExportFollowing()
	{
		var user = HttpContext.GetUserOrFail();

		var followCount = await db.Followings
		                          .CountAsync(p => p.FollowerId == user.Id);
		if (followCount < 1)
			throw GracefulException.BadRequest("You do not follow any users");

		var following = await importExportSvc.ExportFollowingAsync(user);

		return File(Encoding.UTF8.GetBytes(following), "text/csv", $"following-{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.csv");
	}

	[HttpPost("import/following")]
	[EnableRateLimiting("imports")]
	[ProducesResults(HttpStatusCode.Accepted)]
	public async Task<AcceptedResult> ImportFollowing(IFormFile file)
	{
		var user = HttpContext.GetUserOrFail();

		var reader   = new StreamReader(file.OpenReadStream());
		var contents = await reader.ReadToEndAsync();

		var fqns = contents
		           .Split("\n")
		           .Where(line => !string.IsNullOrWhiteSpace(line))
		           .Select(line => line.SplitCommas().First())
		           .Where(fqn => fqn.Contains('@'))
		           .ToList();

		await importExportSvc.ImportFollowingAsync(user, fqns);

		return Accepted();
	}

	private async Task<UserSettings> GetOrInitUserSettings()
	{
		var user     = HttpContext.GetUserOrFail();
		var settings = user.UserSettings;
		if (settings != null) return settings;

		settings = new UserSettings { User = user };
		db.Add(settings);
		await db.SaveChangesAsync();
		await db.ReloadEntityAsync(settings);
		return settings;
	}

	private async Task<TwoFactorEnrollmentResponse> EnrollNewTwoFactorSecret(UserSettings settings, User user)
	{
		settings.TwoFactorTempSecret = TotpHelper.GenerateSecret();
		await db.SaveChangesAsync();

		var secret       = settings.TwoFactorTempSecret;
		var instanceName = await meta.GetAsync(MetaEntity.InstanceName) ?? "Iceshrimp.NET";

		var label  = $"@{user.Username}@{instance.Value.AccountDomain}".Replace(':', '_');
		var issuer = instanceName.Replace(':', '_');
		var url    = $"otpauth://totp/{label.UrlEncode()}?secret={secret}&issuer={issuer.UrlEncode()}";

		using var qrData      = QRCodeGenerator.GenerateQrCode(url, QRCodeGenerator.ECCLevel.Default, true, true);
		using var qrPng       = new PngByteQRCode(qrData);
		var       qrPngBytes  = qrPng.GetGraphic(10, false);
		var       qrPngBase64 = Convert.ToBase64String(qrPngBytes);

		return new TwoFactorEnrollmentResponse
		{
			Secret = settings.TwoFactorTempSecret,
			Url    = url,
			QrPng  = $"data:image/png;base64,{qrPngBase64}"
		};
	}
}