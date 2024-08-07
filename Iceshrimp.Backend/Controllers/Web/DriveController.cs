using System.Net;
using System.Net.Mime;
using Iceshrimp.Backend.Controllers.Shared.Attributes;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Backend.Core.Services;
using Iceshrimp.Shared.Schemas.Web;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
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
	DriveService driveSvc
) : ControllerBase
{
	[EnableCors("drive")]
	[HttpGet("/files/{accessKey}")]
	[ProducesResults(HttpStatusCode.OK, HttpStatusCode.NoContent)]
	[ProducesErrors(HttpStatusCode.NotFound)]
	public async Task<IActionResult> GetFileByAccessKey(string accessKey)
	{
		var file = await db.DriveFiles.FirstOrDefaultAsync(p => p.AccessKey == accessKey ||
		                                                        p.PublicAccessKey == accessKey ||
		                                                        p.ThumbnailAccessUrl == accessKey);
		if (file == null)
		{
			Response.Headers.CacheControl = "max-age=86400";
			throw GracefulException.NotFound("File not found");
		}

		var filename = file.AccessKey == accessKey || file.Name.EndsWith(".webp") ? file.Name : $"{file.Name}.webp";

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

			Response.Headers.CacheControl        = "max-age=31536000, immutable";
			Response.Headers.XContentTypeOptions = "nosniff";
			return File(stream, file.Type, filename, true);
		}
		else
		{
			if (file.IsLink)
			{
				//TODO: handle remove media proxying
				return NoContent();
			}

			var stream = await objectStorage.GetFileAsync(accessKey);
			if (stream == null)
			{
				logger.LogError("Failed to get file {accessKey} from object storage", accessKey);
				throw GracefulException.NotFound("File not found");
			}

			Response.Headers.CacheControl        = "max-age=31536000, immutable";
			Response.Headers.XContentTypeOptions = "nosniff";
			return File(stream, file.Type, filename, true);
		}
	}

	[HttpPost]
	[Authenticate]
	[Authorize]
	[Produces(MediaTypeNames.Application.Json)]
	[ProducesResults(HttpStatusCode.OK)]
	public async Task<DriveFileResponse> UploadFile(IFormFile file)
	{
		var user = HttpContext.GetUserOrFail();
		var request = new DriveFileCreationRequest
		{
			Filename    = file.FileName,
			MimeType    = file.ContentType,
			IsSensitive = false
		};
		var res = await driveSvc.StoreFile(file.OpenReadStream(), user, request);
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
		var file = await db.DriveFiles.FirstOrDefaultAsync(p => p.User == user && p.Id == id) ??
		           throw GracefulException.NotFound("File not found");

		return new DriveFileResponse
		{
			Id           = file.Id,
			Url          = file.AccessUrl,
			ThumbnailUrl = file.ThumbnailAccessUrl,
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
		var file = await db.DriveFiles.FirstOrDefaultAsync(p => p.User == user && p.Id == id) ??
		           throw GracefulException.NotFound("File not found");

		file.Name        = request.Filename ?? file.Name;
		file.IsSensitive = request.Sensitive ?? file.IsSensitive;
		file.Comment     = request.Description;
		await db.SaveChangesAsync();

		return await GetFileById(id);
	}
}