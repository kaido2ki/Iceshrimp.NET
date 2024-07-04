using System.Net.Mime;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Backend.Core.Services;
using Iceshrimp.Shared.Schemas;
using Iceshrimp.Shared.Schemas.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Controllers;

[ApiController]
[Authenticate]
[Authorize]
[EnableRateLimiting("sliding")]
[Route("/api/iceshrimp/emoji")]
[Produces(MediaTypeNames.Application.Json)]
public class EmojiController(
	DatabaseContext db,
	EmojiService emojiSvc
) : ControllerBase
{
	[HttpGet]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<EmojiResponse>))]
	public async Task<IActionResult> GetAllEmoji()
	{
		var res = await db.Emojis
		                  .Where(p => p.Host == null)
		                  .Select(p => new EmojiResponse
		                  {
			                  Id        = p.Id,
			                  Name      = p.Name,
			                  Uri       = p.Uri,
			                  Aliases   = p.Aliases,
			                  Category  = p.Category,
			                  PublicUrl = p.PublicUrl,
			                  License   = p.License
		                  })
		                  .ToListAsync();

		return Ok(res);
	}

	[HttpGet("{id}")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(EmojiResponse))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
	public async Task<IActionResult> GetEmoji(string id)
	{
		var emoji = await db.Emojis.FirstOrDefaultAsync(p => p.Id == id);

		if (emoji == null) return NotFound();

		var res = new EmojiResponse
		{
			Id        = emoji.Id,
			Name      = emoji.Name,
			Uri       = emoji.Uri,
			Aliases   = emoji.Aliases,
			Category  = emoji.Category,
			PublicUrl = emoji.PublicUrl,
			License   = emoji.License
		};

		return Ok(res);
	}

	[HttpPost]
	[Authorize("role:admin")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(EmojiResponse))]
	[ProducesResponseType(StatusCodes.Status409Conflict, Type = typeof(ErrorResponse))]
	public async Task<IActionResult> UploadEmoji(IFormFile file, [FromServices] IOptions<Config.InstanceSection> config)
	{
		var emoji = await emojiSvc.CreateEmojiFromStream(file.OpenReadStream(), file.FileName, file.ContentType,
		                                                 config.Value);

		var res = new EmojiResponse
		{
			Id        = emoji.Id,
			Name      = emoji.Name,
			Uri       = emoji.Uri,
			Aliases   = [],
			Category  = null,
			PublicUrl = emoji.PublicUrl,
			License   = null
		};

		return Ok(res);
	}

	[HttpPost("clone/{name}@{host}")]
	[Authorize("role:admin")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(EmojiResponse))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
	[ProducesResponseType(StatusCodes.Status409Conflict, Type = typeof(ErrorResponse))]
	public async Task<IActionResult> CloneEmoji(string name, string host)
	{
		var localEmojo = await db.Emojis.FirstOrDefaultAsync(e => e.Name == name && e.Host == null);
		if (localEmojo != null) return Conflict();

		var emojo = await db.Emojis.FirstOrDefaultAsync(e => e.Name == name && e.Host == host);
		if (emojo == null) return NotFound();

		return Ok(await emojiSvc.CloneEmoji(emojo));
	}

	[HttpPost("import")]
	[Authorize("role:admin")]
	[ProducesResponseType(StatusCodes.Status202Accepted)]
	public async Task<IActionResult> ImportEmoji(IFormFile file, [FromServices] EmojiImportService emojiImportSvc)
	{
		var zip = await emojiImportSvc.Parse(file.OpenReadStream());
		await emojiImportSvc.Import(zip); // TODO: run in background. this will take a while

		return Accepted();
	}

	[HttpPatch("{id}")]
	[Authorize("role:admin")]
	[Consumes(MediaTypeNames.Application.Json)]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(EmojiResponse))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
	public async Task<IActionResult> UpdateEmoji(
		string id, UpdateEmojiRequest request, [FromServices] IOptions<Config.InstanceSection> config
	)
	{
		var emoji = await emojiSvc.UpdateLocalEmoji(id, request.Name, request.Aliases, request.Category,
		                                            request.License, config.Value);
		if (emoji == null) return NotFound();

		var res = new EmojiResponse
		{
			Id        = emoji.Id,
			Name      = emoji.Name,
			Uri       = emoji.Uri,
			Aliases   = emoji.Aliases,
			Category  = emoji.Category,
			PublicUrl = emoji.PublicUrl,
			License   = emoji.License
		};

		return Ok(res);
	}

	[HttpDelete("{id}")]
	[Authorize("role:admin")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
	public async Task<IActionResult> DeleteEmoji(string id)
	{
		await emojiSvc.DeleteEmoji(id);
		return Ok();
	}
}