using System.Diagnostics.CodeAnalysis;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Services;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Controllers;

[ApiController]
public class DriveController(
	DatabaseContext db,
	ObjectStorageService objectStorage,
	[SuppressMessage("ReSharper", "SuggestBaseTypeForParameterInConstructor")]
	IOptionsSnapshot<Config.StorageSection> options,
	ILogger<DriveController> logger
) : ControllerBase
{
	[EnableCors("drive")]
	[HttpGet("/files/{accessKey}")]
	public async Task<IActionResult> GetFile(string accessKey)
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

			Response.Headers.CacheControl = "max-age=31536000, immutable";
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

			Response.Headers.CacheControl = "max-age=31536000, immutable";
			return File(stream, file.Type, true);
		}
	}
}