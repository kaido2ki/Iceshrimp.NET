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
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QRCoder;

namespace Iceshrimp.Backend.Controllers.Web;

/// <summary>
/// Operations for managing the user's settings and other data.
/// </summary>
[ApiController]
[Authenticate]
[Authorize]
[EnableRateLimiting("sliding")]
[Route("/api/iceshrimp/settings")]
[Produces(MediaTypeNames.Application.Json)]
[EnableCors("iceshrimp")]
public class SettingsController(
	DatabaseContext db,
	DriveService driveSvc,
	ImportExportService importExportSvc,
	MetaService meta,
	IOptions<Config.InstanceSection> instance
) : ControllerBase
{
	/// <summary>
	/// Get settings
	/// </summary>
	/// <response code="200">User settings</response>
	[HttpGet]
	[ProducesResults(HttpStatusCode.OK)]
	public async Task<UserSettingsResponse> GetSettings()
	{
		var settings = await GetOrInitUserSettings();
		var user     = HttpContext.GetUserOrFail();

		var canBite = user.CanBite switch
		{
			Core.Database.Tables.User.BiteControl.Public    => BiteControl.Public,
			Core.Database.Tables.User.BiteControl.Followers => BiteControl.Followers,
			_                                               => BiteControl.None
		};

		return new UserSettingsResponse
		{
			FilterInaccessible      = settings.FilterInaccessible,
			PrivateMode             = settings.PrivateMode,
			AlwaysMarkSensitive     = settings.AlwaysMarkSensitive,
			AutoAcceptFollowed      = settings.AutoAcceptFollowed,
			DefaultNoteVisibility   = (NoteVisibility)settings.DefaultNoteVisibility,
			DefaultRenoteVisibility = (NoteVisibility)settings.DefaultNoteVisibility,
			TwoFactorEnrolled       = settings.TwoFactorEnabled,
			ManuallyAcceptFollows   = user.IsLocked,
			HideRepliesNotFollowing = settings.HideRepliesNotFollowing,
			IsExplorable            = user.IsExplorable,
			CanBite                 = canBite
		};
	}

	/// <summary>
	/// Update settings
	/// </summary>
	/// <param name="newSettings">New settings</param>
	[HttpPut]
	[Consumes(MediaTypeNames.Application.Json)]
	[ProducesResults(HttpStatusCode.OK)]
	public async Task UpdateSettings(UserSettingsRequest newSettings)
	{
		var settings = await GetOrInitUserSettings();

		if (newSettings.DefaultRenoteVisibility == NoteVisibility.Specified)
			throw GracefulException.BadRequest("Default renote visibility cannot be 'specified'");

		settings.FilterInaccessible      = newSettings.FilterInaccessible;
		settings.PrivateMode             = newSettings.PrivateMode;
		settings.AlwaysMarkSensitive     = newSettings.AlwaysMarkSensitive;
		settings.AutoAcceptFollowed      = newSettings.AutoAcceptFollowed;
		settings.HideRepliesNotFollowing = newSettings.HideRepliesNotFollowing;
		settings.DefaultNoteVisibility   = (Note.NoteVisibility)newSettings.DefaultNoteVisibility;
		settings.DefaultRenoteVisibility = (Note.NoteVisibility)newSettings.DefaultRenoteVisibility;

		var user = HttpContext.GetUserOrFail();
		await db.Users.Where(p => p.Id == user.Id)
		        .ExecuteUpdateAsync(p => p.SetProperty(u => u.IsLocked,
		                                               newSettings.ManuallyAcceptFollows || newSettings.PrivateMode));

		await db.Users.Where(p => p.Id == user.Id)
		        .ExecuteUpdateAsync(p => p.SetProperty(u => u.CanBite, newSettings.CanBite switch
		        {
			        BiteControl.Public    => Core.Database.Tables.User.BiteControl.Public,
			        BiteControl.Followers => Core.Database.Tables.User.BiteControl.Followers,
			        _                     => null
		        }));

		await db.Users.Where(p => p.Id == user.Id)
		        .ExecuteUpdateAsync(p => p.SetProperty(u => u.IsExplorable, newSettings.IsExplorable));

		await db.SaveChangesAsync();
	}

	/// <summary>
	/// Enrol two-factor authentication
	/// </summary>
	/// <remarks>Enrol a two-factor authenticator.</remarks>
	/// <response code="200">Two-factor authentication enrolment information</response>
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

	/// <summary>
	/// Re-enroll two-factor authentication
	/// </summary>
	/// <remarks>Replace the current two-factor authenticator with a new authenticator.</remarks>
	/// <param name="request">Two-factor authentication</param>
	/// <response code="200">Two-factor authentication enrolment information</response>
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

	/// <summary>
	/// Confirm two-factor authentication
	/// </summary>
	/// <remarks>Confirm that the two-factor authentication enrolment is valid.</remarks>
	/// <param name="request">Two-factor authentication</param>
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

	/// <summary>
	/// Disable two-factor authentication
	/// </summary>
	/// <param name="request">Two-factor authentication</param>
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

	/// <summary>
	/// Export blocking list
	/// </summary>
	/// <remarks>
	/// Exports the list of users that are blocked as a CSV file in the user's Drive. Formatted as:
	/// <code>@username@host</code>
	/// </remarks>
	/// <response code="200">Drive file metadata</response>
	[HttpPost("export/blocking")]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.BadRequest)]
	public async Task<DriveFileResponse> ExportBlocking()
	{
		var user = HttpContext.GetUserOrFail();

		var existing = await db.Blockings
		                       .AnyAsync(p => p.BlockerId == user.Id);
		if (!existing)
			throw GracefulException.BadRequest("You do not block any users");

		var blocking = await importExportSvc.ExportBlockingAsync(user);

		using var stream = new MemoryStream();
		stream.Write(Encoding.UTF8.GetBytes(blocking));

		var file = await driveSvc.StoreFileAsync(stream, user,
		                                         new DriveFileCreationRequest
		                                         {
			                                         Filename    = $"blocking-{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.csv",
			                                         IsSensitive = false,
			                                         MimeType    = "text/csv"
		                                         });

		return new DriveFileResponse
		{
			Id           = file.Id,
			Url          = file.RawAccessUrl,
			ThumbnailUrl = file.RawThumbnailAccessUrl,
			Filename     = file.Name,
			ContentType  = file.Type,
			Sensitive    = file.IsSensitive,
			Description  = file.Comment,
			IsAvatar     = false,
			IsBanner     = false
		};
	}

	/// <summary>
	/// Export following list
	/// </summary>
	/// <remarks>
	/// Exports the list of users that are being followed as a CSV file in the user's Drive. Formatted as:
	/// <code>@username@host</code>
	/// </remarks>
	/// <response code="200">Drive file metadata</response>
	[HttpPost("export/following")]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.BadRequest)]
	public async Task<DriveFileResponse> ExportFollowing()
	{
		var user = HttpContext.GetUserOrFail();

		var followCount = await db.Followings
		                          .CountAsync(p => p.FollowerId == user.Id);
		if (followCount < 1)
			throw GracefulException.BadRequest("You do not follow any users");

		var following = await importExportSvc.ExportFollowingAsync(user);

		using var stream = new MemoryStream();
		stream.Write(Encoding.UTF8.GetBytes(following));

		var file = await driveSvc.StoreFileAsync(stream, user,
		                                         new DriveFileCreationRequest
		                                         {
			                                         Filename    = $"following-{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.csv",
			                                         IsSensitive = false,
			                                         MimeType    = "text/csv"
		                                         });

		return new DriveFileResponse
		{
			Id           = file.Id,
			Url          = file.RawAccessUrl,
			ThumbnailUrl = file.RawThumbnailAccessUrl,
			Filename     = file.Name,
			ContentType  = file.Type,
			Sensitive    = file.IsSensitive,
			Description  = file.Comment,
			IsAvatar     = false,
			IsBanner     = false
		};
	}

	/// <summary>
	/// Export muting list
	/// </summary>
	/// <remarks>
	/// Exports the list of users that are muted as a CSV file in the user's Drive. Formatted as:
	/// <code>@username@host</code>
	/// </remarks>
	/// <response code="200">Drive file metadata</response>
	[HttpPost("export/muting")]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.BadRequest)]
	public async Task<DriveFileResponse> ExportMuting()
	{
		var user = HttpContext.GetUserOrFail();

		var existing = await db.Mutings
		                       .AnyAsync(p => p.MuterId == user.Id);
		if (!existing)
			throw GracefulException.BadRequest("You do not mute any users");

		var muting = await importExportSvc.ExportMutingAsync(user);

		using var stream = new MemoryStream();
		stream.Write(Encoding.UTF8.GetBytes(muting));

		var file = await driveSvc.StoreFileAsync(stream, user,
		                                         new DriveFileCreationRequest
		                                         {
			                                         Filename    = $"muting-{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.csv",
			                                         IsSensitive = false,
			                                         MimeType    = "text/csv"
		                                         });

		return new DriveFileResponse
		{
			Id           = file.Id,
			Url          = file.RawAccessUrl,
			ThumbnailUrl = file.RawThumbnailAccessUrl,
			Filename     = file.Name,
			ContentType  = file.Type,
			Sensitive    = file.IsSensitive,
			Description  = file.Comment,
			IsAvatar     = false,
			IsBanner     = false
		};
	}

	/// <summary>
	/// Import blocking list
	/// </summary>
	/// <remarks>
	/// Import a list of users that should be blocked from a CSV file. Formatted as:
	/// <code>@username@host</code>
	/// </remarks>
	/// <response code="202">Started import</response>
	[HttpPost("import/blocking")]
	[EnableRateLimiting("imports")]
	[ProducesResults(HttpStatusCode.Accepted)]
	public async Task<AcceptedResult> ImportBlocking(IFormFile file)
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

		await importExportSvc.ImportBlockingAsync(user, fqns);

		return Accepted();
	}

	/// <summary>
	/// Import following list
	/// </summary>
	/// <remarks>
	/// Import a list of users that should be followed from a CSV file. Formatted as:
	/// <code>@username@host</code>
	/// </remarks>
	/// <response code="202">Started import</response>
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

	/// <summary>
	/// Import muting list
	/// </summary>
	/// <remarks>
	/// Import a list of users that should be muted from a CSV file. Formatted as:
	/// <code>@username@host</code>
	/// </remarks>
	/// <response code="202">Started import</response>
	[HttpPost("import/muting")]
	[EnableRateLimiting("imports")]
	[ProducesResults(HttpStatusCode.Accepted)]
	public async Task<AcceptedResult> ImportMuting(IFormFile file)
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

		await importExportSvc.ImportMutingAsync(user, fqns);

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