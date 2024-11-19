using System.Net;
using System.Net.Mime;
using Iceshrimp.Backend.Controllers.Shared.Attributes;
using Iceshrimp.Backend.Controllers.Shared.Schemas;
using Iceshrimp.Backend.Controllers.Web.Renderers;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Backend.Core.Services;
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
[Route("/api/iceshrimp/users")]
[Produces(MediaTypeNames.Application.Json)]
public class UserController(
	DatabaseContext db,
	UserRenderer userRenderer,
	NoteRenderer noteRenderer,
	UserProfileRenderer userProfileRenderer,
	ActivityPub.UserResolver userResolver,
	UserService userSvc,
	BiteService biteSvc,
	IOptions<Config.InstanceSection> config
) : ControllerBase
{
	[HttpGet("{id}")]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.NotFound)]
	public async Task<UserResponse> GetUser(string id)
	{
		var user = await db.Users.IncludeCommonProperties()
		                   .FirstOrDefaultAsync(p => p.Id == id) ??
		           throw GracefulException.NotFound("User not found");

		return await userRenderer.RenderOne(await userResolver.GetUpdatedUserAsync(user));
	}

	[HttpGet("lookup")]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.NotFound)]
	public async Task<UserResponse> LookupUser([FromQuery] string username, [FromQuery] string? host)
	{
		username = username.ToLowerInvariant();
		host     = host?.ToPunycodeLower();

		if (host == config.Value.WebDomain || host == config.Value.AccountDomain)
			host = null;

		var user = await db.Users.IncludeCommonProperties()
		                   .FirstOrDefaultAsync(p => p.UsernameLower == username && p.Host == host) ??
		           throw GracefulException.NotFound("User not found");

		return await userRenderer.RenderOne(await userResolver.GetUpdatedUserAsync(user));
	}

	[HttpGet("{id}/profile")]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.NotFound)]
	public async Task<UserProfileResponse> GetUserProfile(string id)
	{
		var localUser = HttpContext.GetUserOrFail();
		var user = await db.Users.IncludeCommonProperties()
		                   .FirstOrDefaultAsync(p => p.Id == id) ??
		           throw GracefulException.NotFound("User not found");

		return await userProfileRenderer.RenderOne(await userResolver.GetUpdatedUserAsync(user), localUser);
	}

	[HttpGet("{id}/notes")]
	[LinkPagination(20, 80)]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.NotFound)]
	public async Task<IEnumerable<NoteResponse>> GetUserNotes(string id, PaginationQuery pq)
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
		                    .ToListAsync()
		                    .ContinueWithResultAsync(res => res.EnforceRenoteReplyVisibility());

		return await noteRenderer.RenderManyAsync(notes, localUser, Filter.FilterContext.Accounts);
	}

	[HttpPost("{id}/bite")]
	[Authenticate]
	[Authorize]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.BadRequest, HttpStatusCode.NotFound)]
	public async Task BiteUser(string id)
	{
		var user = HttpContext.GetUserOrFail();
		if (user.Id == id)
			throw GracefulException.BadRequest("You cannot bite yourself");

		var target = await db.Users.IncludeCommonProperties().Where(p => p.Id == id).FirstOrDefaultAsync() ??
		             throw GracefulException.NotFound("User not found");

		await biteSvc.BiteAsync(user, target);
	}

	[HttpPost("{id}/follow")]
	[Authenticate]
	[Authorize]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.BadRequest, HttpStatusCode.Forbidden, HttpStatusCode.NotFound)]
	public async Task FollowUser(string id)
	{
		var user = HttpContext.GetUserOrFail();
		if (user.Id == id)
			throw GracefulException.BadRequest("You cannot follow yourself");

		var followee = await db.Users.IncludeCommonProperties()
		                       .Where(p => p.Id == id)
		                       .PrecomputeRelationshipData(user)
		                       .FirstOrDefaultAsync() ??
		               throw GracefulException.NotFound("User not found");

		if ((followee.PrecomputedIsBlockedBy ?? true) || (followee.PrecomputedIsBlocking ?? true))
			throw GracefulException.Forbidden("This action is not allowed");

		if (!(followee.PrecomputedIsFollowedBy ?? false) && !(followee.PrecomputedIsRequestedBy ?? false))
			await userSvc.FollowUserAsync(user, followee);
	}

	[HttpPost("{id}/remove_from_followers")]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.BadRequest, HttpStatusCode.NotFound)]
	public async Task RemoveFromFollowers(string id)
	{
		var user = HttpContext.GetUserOrFail();
		if (user.Id == id)
			throw GracefulException.BadRequest("You cannot unfollow yourself");

		var follower = await db.Followings
		                       .Where(p => p.FolloweeId == user.Id && p.FollowerId == id)
		                       .Select(p => p.Follower)
		                       .PrecomputeRelationshipData(user)
		                       .FirstOrDefaultAsync() ??
		               throw GracefulException.RecordNotFound();

		await userSvc.RemoveFromFollowersAsync(user, follower);
	}

	[HttpPost("{id}/unfollow")]
	[ProducesErrors(HttpStatusCode.BadRequest, HttpStatusCode.NotFound)]
	public async Task UnfollowUser(string id)
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
	}
}