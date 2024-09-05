using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Mime;
using Iceshrimp.Backend.Controllers.Mastodon.Attributes;
using Iceshrimp.Backend.Controllers.Mastodon.Schemas;
using Iceshrimp.Backend.Controllers.Mastodon.Schemas.Entities;
using Iceshrimp.Backend.Controllers.Shared.Attributes;
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
public class MediaController(DriveService driveSvc, DatabaseContext db) : ControllerBase
{
	[MaxRequestSizeIsMaxUploadSize]
	[HttpPost("/api/v1/media")]
	[HttpPost("/api/v2/media")]
	[ProducesResults(HttpStatusCode.OK)]
	public async Task<AttachmentEntity> UploadAttachment(MediaSchemas.UploadMediaRequest request)
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
		return RenderAttachment(file);
	}

	[HttpPut("/api/v1/media/{id}")]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.NotFound)]
	public async Task<AttachmentEntity> UpdateAttachment(
		string id, [FromHybrid] MediaSchemas.UpdateMediaRequest request
	)
	{
		var user = HttpContext.GetUserOrFail();
		var file = await db.DriveFiles.FirstOrDefaultAsync(p => p.Id == id && p.User == user) ??
		           throw GracefulException.RecordNotFound();
		file.Comment = request.Description;
		await db.SaveChangesAsync();

		return RenderAttachment(file);
	}

	[HttpGet("/api/v1/media/{id}")]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.NotFound)]
	public async Task<AttachmentEntity> GetAttachment(string id)
	{
		var user = HttpContext.GetUserOrFail();
		var file = await db.DriveFiles.FirstOrDefaultAsync(p => p.Id == id && p.User == user) ??
		           throw GracefulException.RecordNotFound();

		return RenderAttachment(file);
	}

	[HttpPut("/api/v2/media/{id}")]
	[HttpGet("/api/v2/media/{id}")]
	[ProducesErrors(HttpStatusCode.NotFound)]
	public IActionResult FallbackMediaRoute([SuppressMessage("ReSharper", "UnusedParameter.Global")] string id) =>
		throw GracefulException.NotFound("This endpoint is not implemented, but some clients expect a 404 here.");

	private static AttachmentEntity RenderAttachment(DriveFile file)
	{
		return new AttachmentEntity
		{
			Id          = file.Id,
			Type        = AttachmentEntity.GetType(file.Type),
			Url         = file.AccessUrl,
			Blurhash    = file.Blurhash,
			Description = file.Comment,
			PreviewUrl  = file.ThumbnailAccessUrl,
			RemoteUrl   = file.Uri,
			Sensitive   = file.IsSensitive
			//Metadata    = TODO,
		};
	}
}