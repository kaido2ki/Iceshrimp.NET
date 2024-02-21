using System.Net.Mime;
using Iceshrimp.Backend.Controllers.Mastodon.Attributes;
using Iceshrimp.Backend.Controllers.Mastodon.Schemas;
using Iceshrimp.Backend.Controllers.Mastodon.Schemas.Entities;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Backend.Core.Services;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Iceshrimp.Backend.Controllers.Mastodon;

[MastodonApiController]
[Authenticate]
[Authorize("write:media")]
[EnableCors("mastodon")]
[EnableRateLimiting("sliding")]
[Produces(MediaTypeNames.Application.Json)]
[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AttachmentEntity))]
[ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(MastodonErrorResponse))]
[ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(MastodonErrorResponse))]
public class MediaController(DriveService driveSvc) : ControllerBase
{
	[HttpPost("/api/v1/media")]
	[HttpPost("/api/v2/media")]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(MastodonErrorResponse))]
	public async Task<IActionResult> UploadAttachment(MediaSchemas.UploadMediaRequest request)
	{
		var user = HttpContext.GetUserOrFail();
		var rq = new DriveFileCreationRequest
		{
			Filename    = request.File.FileName,
			IsSensitive = false,
			Comment     = request.Description,
			MimeType    = request.File.ContentType
		};
		var file = await driveSvc.StoreFile(request.File.OpenReadStream(), user, rq);
		var res = new AttachmentEntity
		{
			Id          = file.Id,
			Type        = AttachmentEntity.GetType(file.Type),
			Url         = file.Url,
			Blurhash    = file.Blurhash,
			Description = file.Comment,
			PreviewUrl  = file.ThumbnailUrl,
			RemoteUrl   = file.Uri
			//Metadata = TODO
		};

		return Ok(res);
	}
}