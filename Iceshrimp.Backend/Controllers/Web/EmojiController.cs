using System.Net;
using System.Net.Mime;
using Iceshrimp.Backend.Controllers.Shared.Attributes;
using Iceshrimp.Backend.Controllers.Shared.Schemas;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Backend.Core.Services;
using Iceshrimp.Shared.Helpers;
using Iceshrimp.Shared.Schemas.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Controllers.Web;

[ApiController]
[Authenticate]
[Authorize]
[EnableRateLimiting("sliding")]
[Route("/api/iceshrimp/emoji")]
[Produces(MediaTypeNames.Application.Json)]
public class EmojiController(
	IOptions<Config.InstanceSection> instance,
	DatabaseContext db,
	EmojiService emojiSvc,
	EmojiImportService emojiImportSvc
) : ControllerBase
{
	[HttpGet]
	[ProducesResults(HttpStatusCode.OK)]
	public async Task<IEnumerable<EmojiResponse>> GetAllEmoji()
	{
		return await db.Emojis
					   .Where(p => p.Host == null)
					   .Select(p => new EmojiResponse
					   {
						   Id        = p.Id,
						   Name      = p.Name,
						   Uri       = p.Uri,
						   Aliases   = p.Aliases,
						   Category  = p.Category,
						   PublicUrl = p.GetAccessUrl(instance.Value),
						   License   = p.License,
						   Sensitive = p.Sensitive
					   })
					   .ToListAsync();
	}

	[HttpGet("remote")]
	[Authorize("role:moderator")]
	[RestPagination(100, 500)]
	[ProducesResults(HttpStatusCode.OK)]
	public async Task<PaginationWrapper<List<EmojiResponse>>> GetRemoteEmoji(PaginationQuery pq)
	{
		var res = await db.Emojis
						  .Where(p => p.Host != null)
						  .Select(p => new EmojiResponse
						  {
							  Id        = p.Id,
							  Name      = p.Name,
							  Uri       = p.Uri,
							  Aliases   = p.Aliases,
							  Category  = p.Host,
							  PublicUrl = p.GetAccessUrl(instance.Value),
							  License   = p.License,
							  Sensitive = p.Sensitive
						  })
						  .Paginate(pq, ControllerContext)
						  .ToListAsync();

		return HttpContext.CreatePaginationWrapper(pq, res);
	}

	[HttpGet("remote/{host}")]
	[Authorize("role:moderator")]
	[RestPagination(100, 500)]
	[ProducesResults(HttpStatusCode.OK)]
	public async Task<PaginationWrapper<List<EmojiResponse>>> GetRemoteEmojiByHost(string host, PaginationQuery pq)
	{
		var res = await db.Emojis
						  .Where(p => p.Host == host)
						  .Select(p => new EmojiResponse
						  {
							  Id        = p.Id,
							  Name      = p.Name,
							  Uri       = p.Uri,
							  Aliases   = p.Aliases,
							  Category  = p.Host,
							  PublicUrl = p.GetAccessUrl(instance.Value),
							  License   = p.License,
							  Sensitive = p.Sensitive
						  })
						  .Paginate(pq, ControllerContext)
						  .ToListAsync();

		return HttpContext.CreatePaginationWrapper(pq, res);
	}

	[HttpGet("remote/hosts")]
	[Authorize("role:moderator")]
	[LinkPagination(20, 250)]
	[ProducesResults(HttpStatusCode.OK)]
	public async Task<IEnumerable<EntityWrapper<string>>> GetEmojiHostsAsync(PaginationQuery pq)
	{
		pq.MinId ??= "";
		var res = await db.Emojis.Where(p => p.Host != null)
						  .Select(p => new EntityWrapper<string> { Entity = p.Host!, Id = p.Host! })
						  .Distinct()
						  .Paginate(pq, ControllerContext)
						  .ToListAsync()
						  .ContinueWithResult(p => p.NotNull());

		return res;
	}

	[HttpGet("{id}")]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.NotFound)]
	public async Task<EmojiResponse> GetEmoji(string id)
	{
		var emoji = await db.Emojis.FirstOrDefaultAsync(p => p.Id == id)
					?? throw GracefulException.NotFound("Emoji not found");

		return new EmojiResponse
		{
			Id        = emoji.Id,
			Name      = emoji.Name,
			Uri       = emoji.Uri,
			Aliases   = emoji.Aliases,
			Category  = emoji.Category,
			PublicUrl = emoji.GetAccessUrl(instance.Value),
			License   = emoji.License,
			Sensitive = emoji.Sensitive
		};
	}

	[HttpPost]
	[Authorize("role:moderator")]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.Conflict)]
	public async Task<EmojiResponse> UploadEmoji(IFormFile file, [FromQuery] string name)
	{
		var ext   = Path.HasExtension(file.FileName) ? Path.GetExtension(file.FileName) : "";
		var emoji = await emojiSvc.CreateEmojiFromStreamAsync(file.OpenReadStream(), name + ext, file.ContentType);

		return new EmojiResponse
		{
			Id        = emoji.Id,
			Name      = emoji.Name,
			Uri       = emoji.Uri,
			Aliases   = [],
			Category  = null,
			PublicUrl = emoji.GetAccessUrl(instance.Value),
			License   = null,
			Sensitive = false
		};
	}

	[HttpPost("clone/{name}@{host}")]
	[Authorize("role:moderator")]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.NotFound, HttpStatusCode.Conflict)]
	public async Task<EmojiResponse> CloneEmoji(string name, string host)
	{
		var localEmojo = await db.Emojis.FirstOrDefaultAsync(e => e.Name == name && e.Host == null);
		if (localEmojo != null) throw GracefulException.Conflict("An emoji with that name already exists");

		var emojo = await db.Emojis.FirstOrDefaultAsync(e => e.Name == name && e.Host == host);
		if (emojo == null) throw GracefulException.NotFound("Emoji not found");

		var cloned = await emojiSvc.CloneEmojiAsync(emojo);
		return new EmojiResponse
		{
			Id        = cloned.Id,
			Name      = cloned.Name,
			Uri       = cloned.Uri,
			Aliases   = [],
			Category  = null,
			PublicUrl = cloned.GetAccessUrl(instance.Value),
			License   = null,
			Sensitive = cloned.Sensitive
		};
	}

	[HttpPost("import")]
	[Authorize("role:moderator")]
	[DisableRequestSizeLimit]
	[ProducesResults(HttpStatusCode.Accepted)]
	public async Task<AcceptedResult> ImportEmoji(IFormFile file)
	{
		var zip = await emojiImportSvc.ParseAsync(file.OpenReadStream());
		await emojiImportSvc.ImportAsync(zip); // TODO: run in background. this will take a while
		return Accepted();
	}

	[HttpPatch("{id}")]
	[Authorize("role:moderator")]
	[Consumes(MediaTypeNames.Application.Json)]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.NotFound)]
	public async Task<EmojiResponse> UpdateEmoji(string id, UpdateEmojiRequest request)
	{
		var emoji = await emojiSvc.UpdateLocalEmojiAsync(id, request.Name, request.Aliases, request.Category,
														 request.License, request.Sensitive)
					?? throw GracefulException.NotFound("Emoji not found");

		return new EmojiResponse
		{
			Id        = emoji.Id,
			Name      = emoji.Name,
			Uri       = emoji.Uri,
			Aliases   = emoji.Aliases,
			Category  = emoji.Category,
			PublicUrl = emoji.GetAccessUrl(instance.Value),
			License   = emoji.License,
			Sensitive = emoji.Sensitive
		};
	}

	[HttpDelete("{id}")]
	[Authorize("role:moderator")]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.NotFound)]
	public async Task DeleteEmoji(string id)
	{
		await emojiSvc.DeleteEmojiAsync(id);
	}
}
