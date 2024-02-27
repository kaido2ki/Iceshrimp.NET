using System.Diagnostics.CodeAnalysis;
using System.Net.Mime;
using Iceshrimp.Backend.Controllers.Attributes;
using Iceshrimp.Backend.Controllers.Mastodon.Attributes;
using Iceshrimp.Backend.Controllers.Mastodon.Renderers;
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
[Route("/api/v1/accounts")]
[EnableCors("mastodon")]
[Authenticate]
[EnableRateLimiting("sliding")]
[Produces(MediaTypeNames.Application.Json)]
public class AccountController(
	DatabaseContext db,
	UserRenderer userRenderer,
	NoteRenderer noteRenderer,
	UserService userSvc,
	ActivityPub.UserResolver userResolver
) : ControllerBase
{
	[HttpGet("verify_credentials")]
	[Authorize("read:accounts")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AccountEntity))]
	public async Task<IActionResult> VerifyUserCredentials()
	{
		var user = HttpContext.GetUserOrFail();
		var res  = await userRenderer.RenderAsync(user, user.UserProfile, source: true);
		return Ok(res);
	}

	[HttpGet("{id}")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AccountEntity))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(MastodonErrorResponse))]
	public async Task<IActionResult> GetUser(string id)
	{
		var user = await db.Users.IncludeCommonProperties().FirstOrDefaultAsync(p => p.Id == id) ??
		           throw GracefulException.RecordNotFound();
		var res = await userRenderer.RenderAsync(await userResolver.GetUpdatedUser(user));
		return Ok(res);
	}

	[HttpPost("{id}/follow")]
	[Authorize("write:follows")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(RelationshipEntity))]
	//TODO: [FromHybrid] request (bool reblogs, bool notify, bool languages)
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
		{
			await userSvc.FollowUserAsync(user, followee);

			if (followee.IsLocked)
				followee.PrecomputedIsRequestedBy = true;
			else
				followee.PrecomputedIsFollowedBy = true;
		}

		var res = new RelationshipEntity
		{
			Id                  = followee.Id,
			Following           = followee.PrecomputedIsFollowedBy ?? false,
			FollowedBy          = followee.PrecomputedIsFollowing ?? false,
			Blocking            = followee.PrecomputedIsBlockedBy ?? false,
			BlockedBy           = followee.PrecomputedIsBlocking ?? false,
			Requested           = followee.PrecomputedIsRequestedBy ?? false,
			RequestedBy         = followee.PrecomputedIsRequested ?? false,
			Muting              = followee.PrecomputedIsMutedBy ?? false,
			Endorsed            = false, //FIXME
			Note                = "",    //FIXME
			Notifying           = false, //FIXME
			DomainBlocking      = false, //FIXME
			MutingNotifications = false, //FIXME
			ShowingReblogs      = true   //FIXME
		};

		return Ok(res);
	}

	[HttpPost("{id}/unfollow")]
	[Authorize("write:follows")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(RelationshipEntity))]
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

		var res = new RelationshipEntity
		{
			Id                  = followee.Id,
			Following           = followee.PrecomputedIsFollowedBy ?? false,
			FollowedBy          = followee.PrecomputedIsFollowing ?? false,
			Blocking            = followee.PrecomputedIsBlockedBy ?? false,
			BlockedBy           = followee.PrecomputedIsBlocking ?? false,
			Requested           = followee.PrecomputedIsRequestedBy ?? false,
			RequestedBy         = followee.PrecomputedIsRequested ?? false,
			Muting              = followee.PrecomputedIsMutedBy ?? false,
			Endorsed            = false, //FIXME
			Note                = "",    //FIXME
			Notifying           = false, //FIXME
			DomainBlocking      = false, //FIXME
			MutingNotifications = false, //FIXME
			ShowingReblogs      = true   //FIXME
		};

		return Ok(res);
	}

	[HttpGet("relationships")]
	[Authorize("read:follows")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(RelationshipEntity[]))]
	public async Task<IActionResult> GetRelationships([FromQuery(Name = "id")] List<string> ids)
	{
		var user = HttpContext.GetUserOrFail();

		var users = await db.Users
		                    .Where(p => ids.Contains(p.Id))
		                    .IncludeCommonProperties()
		                    .PrecomputeRelationshipData(user)
		                    .ToListAsync();

		var res = users.Select(u => new RelationshipEntity
		{
			Id                  = u.Id,
			Following           = u.PrecomputedIsFollowedBy ?? false,
			FollowedBy          = u.PrecomputedIsFollowing ?? false,
			Blocking            = u.PrecomputedIsBlockedBy ?? false,
			BlockedBy           = u.PrecomputedIsBlocking ?? false,
			Requested           = u.PrecomputedIsRequestedBy ?? false,
			RequestedBy         = u.PrecomputedIsRequested ?? false,
			Muting              = u.PrecomputedIsMutedBy ?? false,
			Endorsed            = false, //FIXME
			Note                = "",    //FIXME
			Notifying           = false, //FIXME
			DomainBlocking      = false, //FIXME
			MutingNotifications = false, //FIXME
			ShowingReblogs      = true   //FIXME
		});

		return Ok(res);
	}

	[HttpGet("{id}/statuses")]
	[Authorize("read:statuses")]
	[LinkPagination(20, 40)]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<StatusEntity>))]
	public async Task<IActionResult> GetUserStatuses(
		string id, AccountSchemas.AccountStatusesRequest request, MastodonPaginationQuery query
	)
	{
		var user    = HttpContext.GetUserOrFail();
		var account = await db.Users.FirstOrDefaultAsync(p => p.Id == id) ?? throw GracefulException.RecordNotFound();

		var res = await db.Notes
		                  .IncludeCommonProperties()
		                  .FilterByUser(account)
		                  .FilterByAccountStatusesRequest(request, account)
		                  .EnsureVisibleFor(user)
		                  .Paginate(query, ControllerContext)
		                  .PrecomputeVisibilities(user)
		                  .RenderAllForMastodonAsync(noteRenderer, user);

		return Ok(res);
	}

	[HttpGet("{id}/followers")]
	[Authenticate("read:accounts")]
	[LinkPagination(40, 80)]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<AccountEntity>))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(MastodonErrorResponse))]
	public async Task<IActionResult> GetUserFollowers(string id, MastodonPaginationQuery query)
	{
		var user = HttpContext.GetUser();
		var account = await db.Users
		                      .Include(p => p.UserProfile)
		                      .FirstOrDefaultAsync(p => p.Id == id) ??
		              throw GracefulException.RecordNotFound();

		if (user == null || user.Id != account.Id)
		{
			if (account.UserProfile?.FFVisibility == UserProfile.UserProfileFFVisibility.Private)
				return Ok((List<AccountEntity>) []);
			if (account.UserProfile?.FFVisibility == UserProfile.UserProfileFFVisibility.Followers)
				if (user == null || !await db.Users.AnyAsync(p => p == account && p.Followers.Contains(user)))
					return Ok((List<AccountEntity>) []);
		}

		var res = await db.Users
		                  .Where(p => p == account)
		                  .SelectMany(p => p.Followers)
		                  .IncludeCommonProperties()
		                  .Paginate(query, ControllerContext)
		                  .RenderAllForMastodonAsync(userRenderer);

		return Ok(res);
	}

	[HttpGet("{id}/following")]
	[Authenticate("read:accounts")]
	[LinkPagination(40, 80)]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<AccountEntity>))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(MastodonErrorResponse))]
	public async Task<IActionResult> GetUserFollowing(string id, MastodonPaginationQuery query)
	{
		var user = HttpContext.GetUser();
		var account = await db.Users
		                      .Include(p => p.UserProfile)
		                      .FirstOrDefaultAsync(p => p.Id == id) ??
		              throw GracefulException.RecordNotFound();

		if (user == null || user.Id != account.Id)
		{
			if (account.UserProfile?.FFVisibility == UserProfile.UserProfileFFVisibility.Private)
				return Ok((List<AccountEntity>) []);
			if (account.UserProfile?.FFVisibility == UserProfile.UserProfileFFVisibility.Followers)
				if (user == null || !await db.Users.AnyAsync(p => p == account && p.Followers.Contains(user)))
					return Ok((List<AccountEntity>) []);
		}

		var res = await db.Users
		                  .Where(p => p == account)
		                  .SelectMany(p => p.Following)
		                  .IncludeCommonProperties()
		                  .Paginate(query, ControllerContext)
		                  .RenderAllForMastodonAsync(userRenderer);

		return Ok(res);
	}

	[HttpGet("/api/v1/follow_requests")]
	[Authorize("read:follows")]
	[LinkPagination(40, 80)]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<AccountEntity>))]
	public async Task<IActionResult> GetFollowRequests(MastodonPaginationQuery query)
	{
		var user = HttpContext.GetUserOrFail();
		var res = await db.FollowRequests
		                  .Where(p => p.Followee == user)
		                  .IncludeCommonProperties()
		                  .Select(p => p.Follower)
		                  .Paginate(query, ControllerContext)
		                  .RenderAllForMastodonAsync(userRenderer);

		return Ok(res);
	}
	
	[HttpGet("/api/v1/favourites")]
	[Authorize("read:favourites")]
	[LinkPagination(20, 40)]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<StatusEntity>))]
	[SuppressMessage("ReSharper", "EntityFramework.UnsupportedServerSideFunctionCall", Justification = "Projectables")]
	public async Task<IActionResult> GetLikedNotes(MastodonPaginationQuery query)
	{
		var user = HttpContext.GetUserOrFail();
		var res = await db.Notes
		                  .Where(p => db.Users.First(u => u == user).HasLiked(p))
		                  .IncludeCommonProperties()
		                  .Paginate(query, ControllerContext)
		                  .RenderAllForMastodonAsync(noteRenderer, user);

		return Ok(res);
	}
	
	[HttpGet("/api/v1/bookmarks")]
	[Authorize("read:bookmarks")]
	[LinkPagination(20, 40)]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<StatusEntity>))]
	[SuppressMessage("ReSharper", "EntityFramework.UnsupportedServerSideFunctionCall", Justification = "Projectables")]
	public async Task<IActionResult> GetBookmarkedNotes(MastodonPaginationQuery query)
	{
		var user = HttpContext.GetUserOrFail();
		var res = await db.Notes
		                  .Where(p => db.Users.First(u => u == user).HasBookmarked(p))
		                  .IncludeCommonProperties()
		                  .Paginate(query, ControllerContext)
		                  .RenderAllForMastodonAsync(noteRenderer, user);

		return Ok(res);
	}

	[HttpPost("/api/v1/follow_requests/{id}/authorize")]
	[Authorize("write:follows")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(RelationshipEntity))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(MastodonErrorResponse))]
	public async Task<IActionResult> AcceptFollowRequest(string id)
	{
		var user = HttpContext.GetUserOrFail();
		var request = await db.FollowRequests.Where(p => p.Followee == user && p.FollowerId == id)
		                      .Include(p => p.Followee.UserProfile)
		                      .Include(p => p.Follower.UserProfile)
		                      .FirstOrDefaultAsync();

		if (request != null)
			await userSvc.AcceptFollowRequestAsync(request);

		var relationship = await db.Users.Where(p => id == p.Id)
		                           .IncludeCommonProperties()
		                           .PrecomputeRelationshipData(user)
		                           .Select(u => new RelationshipEntity
		                           {
			                           Id                  = u.Id,
			                           Following           = u.PrecomputedIsFollowedBy ?? false,
			                           FollowedBy          = u.PrecomputedIsFollowing ?? false,
			                           Blocking            = u.PrecomputedIsBlockedBy ?? false,
			                           BlockedBy           = u.PrecomputedIsBlocking ?? false,
			                           Requested           = u.PrecomputedIsRequestedBy ?? false,
			                           RequestedBy         = u.PrecomputedIsRequested ?? false,
			                           Muting              = u.PrecomputedIsMutedBy ?? false,
			                           Endorsed            = false, //FIXME
			                           Note                = "",    //FIXME
			                           Notifying           = false, //FIXME
			                           DomainBlocking      = false, //FIXME
			                           MutingNotifications = false, //FIXME
			                           ShowingReblogs      = true   //FIXME
		                           })
		                           .FirstOrDefaultAsync();

		if (relationship == null)
			throw GracefulException.RecordNotFound();

		return Ok(relationship);
	}

	[HttpPost("/api/v1/follow_requests/{id}/reject")]
	[Authorize("write:follows")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(RelationshipEntity))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(MastodonErrorResponse))]
	public async Task<IActionResult> RejectFollowRequest(string id)
	{
		var user = HttpContext.GetUserOrFail();
		var request = await db.FollowRequests.Where(p => p.Followee == user && p.FollowerId == id)
		                      .Include(p => p.Followee.UserProfile)
		                      .Include(p => p.Follower.UserProfile)
		                      .FirstOrDefaultAsync();

		if (request != null)
			await userSvc.RejectFollowRequestAsync(request);

		var relationship = await db.Users.Where(p => id == p.Id)
		                           .IncludeCommonProperties()
		                           .PrecomputeRelationshipData(user)
		                           .Select(u => new RelationshipEntity
		                           {
			                           Id                  = u.Id,
			                           Following           = u.PrecomputedIsFollowedBy ?? false,
			                           FollowedBy          = u.PrecomputedIsFollowing ?? false,
			                           Blocking            = u.PrecomputedIsBlockedBy ?? false,
			                           BlockedBy           = u.PrecomputedIsBlocking ?? false,
			                           Requested           = u.PrecomputedIsRequestedBy ?? false,
			                           RequestedBy         = u.PrecomputedIsRequested ?? false,
			                           Muting              = u.PrecomputedIsMutedBy ?? false,
			                           Endorsed            = false, //FIXME
			                           Note                = "",    //FIXME
			                           Notifying           = false, //FIXME
			                           DomainBlocking      = false, //FIXME
			                           MutingNotifications = false, //FIXME
			                           ShowingReblogs      = true   //FIXME
		                           })
		                           .FirstOrDefaultAsync();

		if (relationship == null)
			throw GracefulException.RecordNotFound();

		return Ok(relationship);
	}

	[HttpGet("lookup")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AccountEntity))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(MastodonErrorResponse))]
	public async Task<IActionResult> LookupUser([FromQuery] string acct)
	{
		var user = await userResolver.LookupAsync(acct) ?? throw GracefulException.RecordNotFound();
		var res  = await userRenderer.RenderAsync(user);
		return Ok(res);
	}
}