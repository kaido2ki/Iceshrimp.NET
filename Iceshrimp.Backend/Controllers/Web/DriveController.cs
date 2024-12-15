using System.Net;
using System.Net.Mime;
using Iceshrimp.Backend.Controllers.Shared.Attributes;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Helpers;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Backend.Core.Queues;
using Iceshrimp.Backend.Core.Services;
using Iceshrimp.Shared.Schemas.Web;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Controllers.Web;

[ApiController]
[Route("/api/iceshrimp/drive")]
public class DriveController(
	DatabaseContext db,
	ObjectStorageService objectStorage,
	IOptionsSnapshot<Config.StorageSection> options,
	ILogger<DriveController> logger,
	DriveService driveSvc,
	QueueService queueSvc,
	HttpClient httpClient
) : ControllerBase
{
	private const string CacheControl = "max-age=31536000, immutable";

	[EnableCors("drive")]
	[EnableRateLimiting("proxy")]
	[HttpGet("/files/{accessKey}/{version?}")]
	[ProducesResults(HttpStatusCode.OK, HttpStatusCode.Redirect)]
	[ProducesErrors(HttpStatusCode.NotFound)]
	public async Task<IActionResult> GetFileByAccessKey(string accessKey, string? version)
	{
		return await GetFileByAccessKey(accessKey, version, null);
	}

	[EnableCors("drive")]
	[EnableRateLimiting("proxy")]
	[HttpGet("/media/emoji/{id}")]
	[ProducesResults(HttpStatusCode.OK, HttpStatusCode.Redirect)]
	[ProducesErrors(HttpStatusCode.NotFound)]
	public async Task<IActionResult> GetEmojiById(string id)
	{
		var emoji = await db.Emojis.FirstOrDefaultAsync(p => p.Id == id)
		            ?? throw GracefulException.NotFound("Emoji not found");

		if (!options.Value.ProxyRemoteMedia || emoji.Host == null)
			return Redirect(emoji.RawPublicUrl);

		return await ProxyAsync(emoji.RawPublicUrl, null, null);
	}

	[EnableCors("drive")]
	[EnableRateLimiting("proxy")]
	[HttpGet("/avatars/{userId}/{version}")]
	[ProducesResults(HttpStatusCode.OK, HttpStatusCode.Redirect)]
	[ProducesErrors(HttpStatusCode.NotFound)]
	public async Task<IActionResult> GetAvatarByUserId(string userId, string? version)
	{
		var user = await db.Users.Include(p => p.Avatar).FirstOrDefaultAsync(p => p.Id == userId)
		           ?? throw GracefulException.NotFound("User not found");

		if (user.Avatar is null)
		{
			var stream = await IdenticonHelper.GetIdenticonAsync(user.Id);
			Response.Headers.CacheControl = CacheControl;
			return new InlineFileStreamResult(stream, "image/png", $"{user.Id}.png", false);
		}

		if (!options.Value.ProxyRemoteMedia)
			return Redirect(user.Avatar.RawThumbnailAccessUrl);

		return await GetFileByAccessKey(user.Avatar.AccessKey, "thumbnail", user.Avatar);
	}

	[EnableCors("drive")]
	[EnableRateLimiting("proxy")]
	[HttpGet("/banners/{userId}/{version}")]
	[ProducesResults(HttpStatusCode.OK, HttpStatusCode.NoContent, HttpStatusCode.Redirect)]
	[ProducesErrors(HttpStatusCode.NotFound)]
	public async Task<IActionResult> GetBannerByUserId(string userId, string? version)
	{
		var user = await db.Users.Include(p => p.Banner).FirstOrDefaultAsync(p => p.Id == userId)
		           ?? throw GracefulException.NotFound("User not found");

		if (user.Banner is null)
			return NoContent();

		if (!options.Value.ProxyRemoteMedia)
			return Redirect(user.Banner.RawThumbnailAccessUrl);

		return await GetFileByAccessKey(user.Banner.AccessKey, "thumbnail", user.Banner);
	}
	
	[EnableCors("drive")]
	[HttpGet("/identicon/{userId}")]
	[HttpGet("/identicon/{userId}.png")]
	[Produces(MediaTypeNames.Image.Png)]
	[ProducesResults(HttpStatusCode.OK, HttpStatusCode.Redirect)]
	[ProducesErrors(HttpStatusCode.NotFound)]
	public async Task<IActionResult> GetIdenticonByUserId(string userId)
	{
		var stream = await IdenticonHelper.GetIdenticonAsync(userId);
		Response.Headers.CacheControl = CacheControl;
		return new InlineFileStreamResult(stream, "image/png", $"{userId}.png", false);
	}

	[HttpPost]
	[Authenticate]
	[Authorize]
	[Produces(MediaTypeNames.Application.Json)]
	[ProducesResults(HttpStatusCode.OK)]
	[MaxRequestSizeIsMaxUploadSize]
	public async Task<DriveFileResponse> UploadFile(IFormFile file)
	{
		var user = HttpContext.GetUserOrFail();
		var request = new DriveFileCreationRequest
		{
			Filename    = file.FileName,
			MimeType    = file.ContentType,
			IsSensitive = false
		};
		var res = await driveSvc.StoreFileAsync(file.OpenReadStream(), user, request);
		return await GetFileById(res.Id);
	}

	[HttpGet("{id}")]
	[Authenticate]
	[Authorize]
	[Produces(MediaTypeNames.Application.Json)]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.NotFound)]
	public async Task<DriveFileResponse> GetFileById(string id)
	{
		var user = HttpContext.GetUserOrFail();
		var file = await db.DriveFiles.FirstOrDefaultAsync(p => p.User == user && p.Id == id)
		           ?? throw GracefulException.NotFound("File not found");

		return new DriveFileResponse
		{
			Id           = file.Id,
			Url          = file.RawAccessUrl,
			ThumbnailUrl = file.RawThumbnailAccessUrl,
			Filename     = file.Name,
			ContentType  = file.Type,
			Description  = file.Comment,
			Sensitive    = file.IsSensitive
		};
	}

	[HttpGet("/by-hash/{sha256}")]
	[Authenticate]
	[Authorize]
	[Produces(MediaTypeNames.Application.Json)]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.NotFound)]
	public async Task<DriveFileResponse> GetFileByHash(string sha256)
	{
		var user = HttpContext.GetUserOrFail();
		var file = await db.DriveFiles.FirstOrDefaultAsync(p => p.User == user && p.Sha256 == sha256)
		           ?? throw GracefulException.NotFound("File not found");

		return new DriveFileResponse
		{
			Id           = file.Id,
			Url          = file.RawAccessUrl,
			ThumbnailUrl = file.RawThumbnailAccessUrl,
			Filename     = file.Name,
			ContentType  = file.Type,
			Description  = file.Comment,
			Sensitive    = file.IsSensitive
		};
	}

	[HttpPatch("{id}")]
	[Authenticate]
	[Authorize]
	[Consumes(MediaTypeNames.Application.Json)]
	[Produces(MediaTypeNames.Application.Json)]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.NotFound)]
	public async Task<DriveFileResponse> UpdateFile(string id, UpdateDriveFileRequest request)
	{
		var user = HttpContext.GetUserOrFail();
		var file = await db.DriveFiles.FirstOrDefaultAsync(p => p.User == user && p.Id == id)
		           ?? throw GracefulException.NotFound("File not found");

		file.Name        = request.Filename ?? file.Name;
		file.IsSensitive = request.Sensitive ?? file.IsSensitive;
		file.Comment     = request.Description;
		await db.SaveChangesAsync();

		return await GetFileById(id);
	}

	[HttpDelete("{id}")]
	[Authenticate]
	[Authorize]
	[ProducesResults(HttpStatusCode.Accepted)]
	[ProducesErrors(HttpStatusCode.NotFound, HttpStatusCode.UnprocessableEntity)]
	public async Task<IActionResult> DeleteFile(string id)
	{
		var user = HttpContext.GetUserOrFail();
		var file = await db.DriveFiles.FirstOrDefaultAsync(p => p.User == user && p.Id == id)
		           ?? throw GracefulException.NotFound("File not found");

		if (await db.Users.AnyAsync(p => p.Avatar == file || p.Banner == file))
			throw GracefulException.UnprocessableEntity("Refusing to delete file: used in banner or avatar");
		if (await db.Notes.AnyAsync(p => p.FileIds.Contains(file.Id)))
			throw GracefulException.UnprocessableEntity("Refusing to delete file: used in note");

		await queueSvc.BackgroundTaskQueue.EnqueueAsync(new DriveFileDeleteJobData
		{
			DriveFileId = file.Id, Expire = false
		});

		return StatusCode(StatusCodes.Status202Accepted);
	}

	[HttpGet("folder")]
	[Authenticate]
	[Authorize]
	[ProducesResults(HttpStatusCode.OK)]
	public async Task<DriveFolderResponse> GetRootFolder()
	{
		return await GetFolder(null);
	}

	[HttpGet("folder/{id}")]
	[Authenticate]
	[Authorize]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.NotFound)]
	public async Task<DriveFolderResponse> GetFolder(string? id)
	{
		var user = HttpContext.GetUserOrFail();

		var folder = id != null
			? await db.DriveFolders.FirstOrDefaultAsync(p => p.Id == id && p.UserId == user.Id)
			  ?? throw GracefulException.RecordNotFound()
			: null;

		var driveFiles = await db.DriveFiles
		                         .Where(p => p.FolderId == id && p.UserId == user.Id)
		                         .Select(p => new DriveFileResponse
		                         {
			                         Id           = p.Id,
			                         Url          = p.AccessUrl,
			                         ThumbnailUrl = p.ThumbnailAccessUrl,
			                         Filename     = p.Name,
			                         ContentType  = p.Type,
			                         Sensitive    = p.IsSensitive,
			                         Description  = p.Comment
		                         })
		                         .ToListAsync();

		var driveFolders = await db.DriveFolders
		                           .Where(p => p.ParentId == id && p.UserId == user.Id)
		                           .Select(p => new DriveFolderResponse
		                           {
			                           Id = p.Id, Name = p.Name, ParentId = p.ParentId
		                           })
		                           .ToListAsync();

		return new DriveFolderResponse
		{
			Id       = folder?.Id,
			Name     = folder?.Name,
			ParentId = folder?.ParentId,
			Files    = driveFiles,
			Folders  = driveFolders
		};
	}

	private async Task<IActionResult> GetFileByAccessKey(string accessKey, string? version, DriveFile? file)
	{
		file ??= await db.DriveFiles.FirstOrDefaultAsync(p => p.AccessKey == accessKey
		                                                      || p.PublicAccessKey == accessKey
		                                                      || p.ThumbnailAccessKey == accessKey);
		if (file == null)
		{
			Response.Headers.CacheControl = "max-age=86400";
			throw GracefulException.NotFound("File not found");
		}

		if (file.IsLink)
		{
			var fetchUrl = version is "thumbnail"
				? file.RawThumbnailAccessUrl
				: file.RawAccessUrl;

			if (!options.Value.ProxyRemoteMedia)
				return Redirect(fetchUrl);

			try
			{
				var filename = file.AccessKey == accessKey || file.Name.EndsWith(".webp")
					? file.Name
					: $"{file.Name}.webp";

				return await ProxyAsync(fetchUrl, file.Type, filename);
			}
			catch (Exception e) when (e is not GracefulException)
			{
				throw GracefulException.BadGateway($"Failed to proxy request: {e.Message}", suppressLog: true);
			}
		}

		if (file.StoredInternal)
		{
			var pathBase = options.Value.Local?.Path;
			if (string.IsNullOrWhiteSpace(pathBase))
			{
				logger.LogError("Failed to get file {accessKey} from local storage: path does not exist", accessKey);
				throw GracefulException.NotFound("File not found");
			}

			var path   = Path.Join(pathBase, accessKey);
			var stream = System.IO.File.OpenRead(path);

			Response.Headers.CacheControl        = CacheControl;
			Response.Headers.XContentTypeOptions = "nosniff";

			return Constants.BrowserSafeMimeTypes.Contains(file.Type)
				? new InlineFileStreamResult(stream, file.Type, file.Name, true)
				: File(stream, file.Type, file.Name, true);
		}
		else
		{
			var stream = await objectStorage.GetFileAsync(accessKey);
			if (stream == null)
			{
				logger.LogError("Failed to get file {accessKey} from object storage", accessKey);
				throw GracefulException.NotFound("File not found");
			}

			Response.Headers.CacheControl        = CacheControl;
			Response.Headers.XContentTypeOptions = "nosniff";

			return Constants.BrowserSafeMimeTypes.Contains(file.Type)
				? new InlineFileStreamResult(stream, file.Type, file.Name, true)
				: File(stream, file.Type, file.Name, true);
		}
	}

	private async Task<IActionResult> ProxyAsync(string url, string? expectedMediaType, string? filename)
	{
		try
		{
			// @formatter:off
			var res = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
			if (!res.IsSuccessStatusCode)
				throw GracefulException.BadGateway($"Failed to proxy request: response status was {res.StatusCode}", suppressLog: true);
			if (res.Content.Headers.ContentType?.MediaType is not { } mediaType)
				throw GracefulException.BadGateway("Failed to proxy request: remote didn't return Content-Type");
			if (expectedMediaType != null && mediaType != expectedMediaType && !Constants.BrowserSafeMimeTypes.Contains(mediaType))
				throw GracefulException.BadGateway("Failed to proxy request: content type mismatch", suppressLog: true);
			// @formatter:on

			Response.Headers.CacheControl        = CacheControl;
			Response.Headers.XContentTypeOptions = "nosniff";

			var stream = await res.Content.ReadAsStreamAsync();

			return Constants.BrowserSafeMimeTypes.Contains(mediaType)
				? new InlineFileStreamResult(stream, mediaType, filename, true)
				: File(stream, mediaType, filename, true);
		}
		catch (Exception e) when (e is not GracefulException)
		{
			throw GracefulException.BadGateway($"Failed to proxy request: {e.Message}", suppressLog: true);
		}
	}
}
