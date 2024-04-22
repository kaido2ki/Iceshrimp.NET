using System.Diagnostics.CodeAnalysis;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Backend.Core.Services;
using Iceshrimp.Shared.Schemas;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Controllers;

[ApiController]
[Route("/api/iceshrimp/drive")]
public class DriveController(
	DatabaseContext db,
	ObjectStorageService objectStorage,
	[SuppressMessage("ReSharper", "SuggestBaseTypeForParameterInConstructor")]
	IOptionsSnapshot<Config.StorageSection> options,
	ILogger<DriveController> logger,
	DriveService driveSvc
) : ControllerBase
{
	[EnableCors("drive")]
	[HttpGet("/files/{accessKey}")]
	public async Task<IActionResult> GetFileByAccessKey(string accessKey)
	{
		var file = await db.DriveFiles.FirstOrDefaultAsync(p => p.AccessKey == accessKey ||
		                                                        p.WebpublicAccessKey == accessKey ||
		                                                        p.ThumbnailAccessKey == accessKey);
		if (file == null)
		{
			Response.Headers.CacheControl = "max-age=86400";
			return NotFound();
		}

		if (file.StoredInternal)
		{
			var pathBase = options.Value.Local?.Path;
			if (string.IsNullOrWhiteSpace(pathBase))
			{
				logger.LogError("Failed to get file {accessKey} from local storage: path does not exist", accessKey);
				return NotFound();
			}

			var path   = Path.Join(pathBase, accessKey);
			var stream = System.IO.File.OpenRead(path);

			Response.Headers.CacheControl        = "max-age=31536000, immutable";
			Response.Headers.XContentTypeOptions = "nosniff";
			return File(stream, file.Type, true);
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
				return NotFound();
			}

			Response.Headers.CacheControl        = "max-age=31536000, immutable";
			Response.Headers.XContentTypeOptions = "nosniff";
			return File(stream, file.Type, true);
		}
	}

	[HttpPost]
	[Authenticate]
	[Authorize]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DriveFileResponse))]
	public async Task<IActionResult> UploadFile([FromForm] IFormFile file)
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
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DriveFileResponse))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
	public async Task<IActionResult> GetFileById(string id)
	{
		var user = HttpContext.GetUserOrFail();
		var file = await db.DriveFiles.FirstOrDefaultAsync(p => p.User == user && p.Id == id);
		if (file == null) return NotFound();

		var res = new DriveFileResponse
		{
			Id           = file.Id,
			Url          = file.PublicUrl,
			ThumbnailUrl = file.PublicThumbnailUrl,
			Filename     = file.Name,
			ContentType  = file.Type,
			Description  = file.Comment,
			Sensitive    = file.IsSensitive
		};

		return Ok(res);
	}

	[HttpPatch("{id}")]
	[Authenticate]
	[Authorize]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DriveFileResponse))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
	public async Task<IActionResult> UpdateFile(string id, UpdateDriveFileRequest request)
	{
		var user = HttpContext.GetUserOrFail();
		var file = await db.DriveFiles.FirstOrDefaultAsync(p => p.User == user && p.Id == id);
		if (file == null) return NotFound();

		file.Name        = request.Filename ?? file.Name;
		file.IsSensitive = request.Sensitive ?? file.IsSensitive;
		file.Comment     = request.Description;
		await db.SaveChangesAsync();

		return await GetFileById(id);
	}
}