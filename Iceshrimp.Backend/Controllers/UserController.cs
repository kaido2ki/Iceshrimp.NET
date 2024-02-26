using System.Net.Mime;
using Iceshrimp.Backend.Controllers.Attributes;
using Iceshrimp.Backend.Controllers.Renderers;
using Iceshrimp.Backend.Controllers.Schemas;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Middleware;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Controllers;

[ApiController]
[EnableRateLimiting("sliding")]
[Route("/api/iceshrimp/v1/user/{id}")]
[Produces(MediaTypeNames.Application.Json)]
public class UserController(
	DatabaseContext db,
	UserRenderer userRenderer,
	NoteRenderer noteRenderer,
	ActivityPub.UserResolver userResolver
) : ControllerBase
{
	[HttpGet]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserResponse))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
	public async Task<IActionResult> GetUser(string id)
	{
		var user = await db.Users.IncludeCommonProperties()
		                   .FirstOrDefaultAsync(p => p.Id == id) ??
		           throw GracefulException.NotFound("User not found");

		return Ok(userRenderer.RenderOne(await userResolver.GetUpdatedUser(user)));
	}

	[HttpGet("notes")]
	[Authenticate]
	[LinkPagination(20, 80)]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<NoteResponse>))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
	public async Task<IActionResult> GetUserNotes(string id, PaginationQuery pq)
	{
		var localUser = HttpContext.GetUser();
		var user = await db.Users.FirstOrDefaultAsync(p => p.Id == id) ??
		           throw GracefulException.NotFound("User not found");

		var notes = await db.Notes
		                    .IncludeCommonProperties()
		                    .Where(p => p.User == user)
		                    .EnsureVisibleFor(localUser)
		                    .PrecomputeVisibilities(localUser)
		                    .Paginate(pq, ControllerContext)
		                    .ToListAsync();

		return Ok(noteRenderer.RenderMany(notes.EnforceRenoteReplyVisibility()));
	}
}