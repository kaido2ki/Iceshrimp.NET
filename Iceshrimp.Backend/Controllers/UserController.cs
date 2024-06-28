using System.Net.Mime;
using Iceshrimp.Backend.Controllers.Attributes;
using Iceshrimp.Backend.Controllers.Renderers;
using Iceshrimp.Backend.Controllers.Schemas;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Backend.Core.Services;
using Iceshrimp.Shared.Schemas;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Controllers;

[ApiController]
[Authenticate]
[Authorize]
[EnableRateLimiting("sliding")]
[Route("/api/iceshrimp/users")]
[Produces(MediaTypeNames.Application.Json)]
public class UserController(
	DatabaseContext db,
	UserRenderer userRenderer,
	NoteRenderer noteRenderer,
	UserProfileRenderer userProfileRenderer,
	ActivityPub.UserResolver userResolver,
	UserService userSvc
) : ControllerBase
{
	[HttpGet("{id}")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserResponse))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
	public async Task<IActionResult> GetUser(string id)
	{
		var user = await db.Users.IncludeCommonProperties()
		                   .FirstOrDefaultAsync(p => p.Id == id) ??
		           throw GracefulException.NotFound("User not found");

		return Ok(await userRenderer.RenderOne(await userResolver.GetUpdatedUser(user)));
	}

	[HttpGet("lookup")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserResponse))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
	public async Task<IActionResult> LookupUser([FromQuery] string username, [FromQuery] string? host)
	{
		username = username.ToLowerInvariant();
		host     = host?.ToLowerInvariant();

		var user = await db.Users.IncludeCommonProperties()
		                   .FirstOrDefaultAsync(p => p.UsernameLower == username && p.Host == host) ??
		           throw GracefulException.NotFound("User not found");

		return Ok(await userRenderer.RenderOne(await userResolver.GetUpdatedUser(user)));
	}

	[HttpGet("{id}/profile")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserProfileResponse))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
	public async Task<IActionResult> GetUserProfile(string id)
	{
		var localUser = HttpContext.GetUserOrFail();
		var user = await db.Users.IncludeCommonProperties()
		                   .FirstOrDefaultAsync(p => p.Id == id) ??
		           throw GracefulException.NotFound("User not found");

		return Ok(await userProfileRenderer.RenderOne(await userResolver.GetUpdatedUser(user), localUser));
	}

	[HttpGet("{id}/notes")]
	[LinkPagination(20, 80)]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<NoteResponse>))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
	public async Task<IActionResult> GetUserNotes(string id, PaginationQuery pq)
	{
		var localUser = HttpContext.GetUserOrFail();
		var user = await db.Users.FirstOrDefaultAsync(p => p.Id == id) ??
		           throw GracefulException.NotFound("User not found");

		var notes = await db.Notes
		                    .IncludeCommonProperties()
		                    .FilterByUser(user)
		                    .EnsureVisibleFor(localUser)
		                    .FilterHidden(localUser, db, filterMutes: false)
		                    .Paginate(pq, ControllerContext)
		                    .PrecomputeVisibilities(localUser)
		                    .ToListAsync();

		return Ok(await noteRenderer.RenderMany(notes.EnforceRenoteReplyVisibility(), localUser,
		                                        Filter.FilterContext.Accounts));
	}

	[HttpPost("{id}/follow")]
	[Authenticate]
	[Authorize]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
	[ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ErrorResponse))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
	public async Task<IActionResult> FollowUser(string id)
	{
		var user = HttpContext.GetUserOrFail();
		if (user.Id == id)
			throw GracefulException.BadRequest("You cannot follow yourself");

		var followee = await db.Users.IncludeCommonProperties()
		                       .Where(p => p.Id == id)
		                       .PrecomputeRelationshipData(user)
		                       .FirstOrDefaultAsync() ??
		               throw GracefulException.RecordNotFound();

		if ((followee.PrecomputedIsBlockedBy ?? true) || (followee.PrecomputedIsBlocking ?? true))
			throw GracefulException.Forbidden("This action is not allowed");

		if (!(followee.PrecomputedIsFollowedBy ?? false) && !(followee.PrecomputedIsRequestedBy ?? false))
			await userSvc.FollowUserAsync(user, followee);

		return Ok();
	}

	[HttpPost("{id}/unfollow")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
	public async Task<IActionResult> UnfollowUser(string id)
	{
		var user = HttpContext.GetUserOrFail();
		if (user.Id == id)
			throw GracefulException.BadRequest("You cannot unfollow yourself");

		var followee = await db.Users
		                       .Where(p => p.Id == id)
		                       .IncludeCommonProperties()
		                       .PrecomputeRelationshipData(user)
		                       .FirstOrDefaultAsync() ??
		               throw GracefulException.RecordNotFound();

		await userSvc.UnfollowUserAsync(user, followee);

		return Ok();
	}
}