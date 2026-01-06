using System.Net;
using System.Net.Mime;
using Iceshrimp.Backend.Controllers.Shared.Attributes;
using Iceshrimp.Backend.Controllers.Shared.Schemas;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Backend.Core.Services;
using Iceshrimp.Shared.Helpers;
using Iceshrimp.Shared.Schemas.Web;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Controllers.Web;

/// <summary>
/// Operations for getting and managing emojis.
/// </summary>
[ApiController]
[Authenticate]
[Authorize]
[EnableRateLimiting("sliding")]
[Route("/api/iceshrimp/emoji")]
[EnableCors("iceshrimp")]
[Produces(MediaTypeNames.Application.Json)]
public class EmojiController(
	IOptions<Config.InstanceSection> instance,
	DatabaseContext db,
	EmojiService emojiSvc,
	EmojiImportService emojiImportSvc
) : ControllerBase
{
	/// <summary>
	/// List emojis
	/// </summary>
	/// <remarks>Returns a list of <b>local</b> emojis.</remarks>
	/// <response code="200">List of emojis</response>
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
			               Tags      = p.Tags,
			               Category  = p.Category,
			               PublicUrl = p.GetAccessUrl(instance.Value),
			               License   = p.License,
			               Sensitive = p.Sensitive
		               })
		               .ToListAsync();
	}

	/// <summary>
	/// Search remote emojis
	/// </summary>
	/// <remarks>
	/// <para>Returns a paginated list of remote emojis matching the search.</para>
	/// <para>Requires role: <b>Moderator</b></para>
	/// </remarks>
	/// <param name="name">Name search</param>
	/// <param name="host">Instance domain name search</param>
	/// <param name="pq">Pagination query</param>
	/// <response code="200">Paginated list of emojis</response>
	[HttpGet("remote")]
	[Authorize("role:moderator")]
	[RestPagination(100, 500)]
	[ProducesResults(HttpStatusCode.OK)]
	public async Task<PaginationWrapper<List<EmojiResponse>>> GetRemoteEmoji(
		[FromQuery] string? name, [FromQuery] string? host, PaginationQuery pq
	)
	{
		var res = await db.Emojis
		                  .Where(p => p.Host != null
		                              && (string.IsNullOrWhiteSpace(host) || p.Host.ToLower().Contains(host.ToLower()))
		                              && (string.IsNullOrWhiteSpace(name) || p.Name.ToLower().Contains(name.ToLower())))
		                  .Select(p => new EmojiResponse
		                  {
			                  Id        = p.Id,
			                  Name      = p.Name,
			                  Uri       = p.Uri,
			                  Tags      = p.Tags,
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
			                  Tags      = p.Tags,
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

	/// <summary>
	/// Get emoji
	/// </summary>
	/// <param name="id">The emoji's ID</param>
	/// <response code="200">Emoji</response>
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
			Tags      = emoji.Tags,
			Category  = emoji.Category,
			PublicUrl = emoji.GetAccessUrl(instance.Value),
			License   = emoji.License,
			Sensitive = emoji.Sensitive
		};
	}

	/// <summary>
	/// Upload emoji
	/// </summary>
	/// <remarks>Requires role: <b>Moderator</b></remarks>
	/// <param name="file">Image file</param>
	/// <param name="name">Emoji name</param>
	/// <response code="200">Emoji</response>
	/// <response code="409">An emoji with that name already exists</response>
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
			Tags      = [],
			Category  = null,
			PublicUrl = emoji.GetAccessUrl(instance.Value),
			License   = null,
			Sensitive = false
		};
	}

	/// <summary>
	/// Clone remote emoji
	/// </summary>
	/// <remarks>
	/// <para>Create a <b>local</b> copy of a <b>remote</b> emoji.</para>
	/// <para>Requires role: <b>Moderator</b></para>
	/// </remarks>
	/// <param name="name">Emoji name</param>
	/// <param name="host">Instance domain name</param>
	/// <response code="200">Emoji</response>
	/// <response code="409">An emoji with that name already exists</response>
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
			Tags      = [],
			Category  = null,
			PublicUrl = cloned.GetAccessUrl(instance.Value),
			License   = null,
			Sensitive = cloned.Sensitive
		};
	}

	/// <summary>
	/// Import emoji pack
	/// </summary>
	/// <remarks>
	/// <para>Import a Misskey-style emoji pack.</para>
	/// <para>Requires role: <b>Moderator</b></para>
	/// </remarks>
	/// <param name="file">Emoji pack ZIP file</param>
	/// <response code="202">Import started</response>
	[HttpPost("import")]
	[Authorize("role:moderator")]
	[NoRequestSizeLimit]
	[ProducesResults(HttpStatusCode.Accepted)]
	public async Task<AcceptedResult> ImportEmoji(IFormFile file)
	{
		var zip = await EmojiImportService.ParseAsync(file.OpenReadStream());
		await emojiImportSvc.ImportAsync(zip); // TODO: run in background. this will take a while
		return Accepted();
	}

	/// <summary>
	/// Update emoji
	/// </summary>
	/// <remarks>Requires role: <b>Moderator</b></remarks>
	/// <param name="id">The emoji's ID</param>
	/// <param name="request">Update emoji request. Fields that are not present are not updated. Fields that are empty are reset.</param>
	/// <response code="200">Updated emoji</response>
	[HttpPatch("{id}")]
	[Authorize("role:moderator")]
	[Consumes(MediaTypeNames.Application.Json)]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.BadRequest, HttpStatusCode.NotFound)]
	public async Task<EmojiResponse> UpdateEmoji(string id, UpdateEmojiRequest request)
	{
		var emoji = await emojiSvc.UpdateLocalEmojiAsync(id, request.Name, request.Tags, request.Category,
		                                                 request.License, request.Sensitive)
		            ?? throw GracefulException.NotFound("Emoji not found");

		return new EmojiResponse
		{
			Id        = emoji.Id,
			Name      = emoji.Name,
			Uri       = emoji.Uri,
			Tags      = emoji.Tags,
			Category  = emoji.Category,
			PublicUrl = emoji.GetAccessUrl(instance.Value),
			License   = emoji.License,
			Sensitive = emoji.Sensitive
		};
	}

	/// <summary>
	/// Delete emoji
	/// </summary>
	/// <remarks>Requires role: <b>Moderator</b></remarks>
	/// <param name="id">The emoji's ID</param>
	[HttpDelete("{id}")]
	[Authorize("role:moderator")]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.NotFound)]
	public async Task DeleteEmoji(string id)
	{
		await emojiSvc.DeleteEmojiAsync(id);
	}

	/// <summary>
	/// Batch update emojis
	/// </summary>
	/// <remarks>
	/// <para>Update a group of emojis at once.</para>
	/// <para>Requires role: <b>Moderator</b></para>
	/// </remarks>
	/// <param name="request">Batch update emoji request. Fields that are not present are not updated. Fields that are empty are reset.</param>
	/// <response code="200">List of updated emoji</response>
	[HttpPatch("batch")]
	[Authorize("role:moderator")]
	[Consumes(MediaTypeNames.Application.Json)]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.BadRequest, HttpStatusCode.NotFound)]
	public async Task<List<EmojiResponse>> BatchUpdateEmoji(BatchUpdateEmojiRequest request)
	{
		var ids = await db.Emojis.Where(p => request.Ids.Contains(p.Id) && p.Host == null)
		                  .Select(p => p.Id)
		                  .ToListAsync();

		if (ids.Count == 0) throw GracefulException.BadRequest("No valid emoji ids were provided");

		var emojis = new List<Emoji>();

		foreach (var id in ids)
		{
			var emoji = await emojiSvc.UpdateLocalEmojiAsync(id, null, null, request.Category,
			                                                 request.License, request.Sensitive);
			if (emoji != null) emojis.Add(emoji);
		}

		return emojis.Select(p => new EmojiResponse
		             {
			             Id        = p.Id,
			             Name      = p.Name,
			             Uri       = p.Uri,
			             Tags      = p.Tags,
			             Category  = p.Category,
			             PublicUrl = p.GetAccessUrl(instance.Value),
			             License   = p.License,
			             Sensitive = p.Sensitive
		             })
		             .ToList();
	}
}
