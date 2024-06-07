using System.Diagnostics.CodeAnalysis;
using System.Net.Mime;
using Iceshrimp.Backend.Controllers.Mastodon.Attributes;
using Iceshrimp.Backend.Controllers.Mastodon.Schemas;
using Iceshrimp.Backend.Controllers.Mastodon.Schemas.Entities;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Backend.Core.Services;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Controllers.Mastodon;

[MastodonApiController]
[Authenticate]
[Authorize("write:media")]
[EnableCors("mastodon")]
[EnableRateLimiting("sliding")]
[Produces(MediaTypeNames.Application.Json)]
[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AttachmentEntity))]
public class MediaController(DriveService driveSvc, DatabaseContext db) : ControllerBase
{
	[HttpPost("/api/v1/media")]
	[HttpPost("/api/v2/media")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AttachmentEntity))]
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
		var res  = RenderAttachment(file);

		return Ok(res);
	}

	[HttpPut("/api/v1/media/{id}")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AttachmentEntity))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(MastodonErrorResponse))]
	public async Task<IActionResult> UpdateAttachment(string id, [FromHybrid] MediaSchemas.UpdateMediaRequest request)
	{
		var user = HttpContext.GetUserOrFail();
		var file = await db.DriveFiles.FirstOrDefaultAsync(p => p.Id == id && p.User == user) ??
		           throw GracefulException.RecordNotFound();
		file.Comment = request.Description;
		await db.SaveChangesAsync();

		var res = RenderAttachment(file);
		return Ok(res);
	}

	[HttpGet("/api/v1/media/{id}")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AttachmentEntity))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(MastodonErrorResponse))]
	public async Task<IActionResult> GetAttachment(string id)
	{
		var user = HttpContext.GetUserOrFail();
		var file = await db.DriveFiles.FirstOrDefaultAsync(p => p.Id == id && p.User == user) ??
		           throw GracefulException.RecordNotFound();

		var res = RenderAttachment(file);
		return Ok(res);
	}

	[HttpPut("/api/v2/media/{id}")]
	[HttpGet("/api/v2/media/{id}")]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(MastodonErrorResponse))]
	public IActionResult FallbackMediaRoute([SuppressMessage("ReSharper", "UnusedParameter.Global")] string id) =>
		throw GracefulException.NotFound("This endpoint is not implemented, but some clients expect a 404 here.");

	private static AttachmentEntity RenderAttachment(DriveFile file)
	{
		return new AttachmentEntity
		{
			Id          = file.Id,
			Type        = AttachmentEntity.GetType(file.Type),
			Url         = file.PublicUrl,
			Blurhash    = file.Blurhash,
			Description = file.Comment,
			PreviewUrl  = file.PublicThumbnailUrl,
			RemoteUrl   = file.Uri,
			Sensitive   = file.IsSensitive,
			//Metadata    = TODO,
		};
	}
}