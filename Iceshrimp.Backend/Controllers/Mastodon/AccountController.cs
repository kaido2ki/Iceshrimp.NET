using System.Diagnostics.CodeAnalysis;
using System.Net.Mime;
using Iceshrimp.Backend.Controllers.Attributes;
using Iceshrimp.Backend.Controllers.Mastodon.Attributes;
using Iceshrimp.Backend.Controllers.Mastodon.Renderers;
using Iceshrimp.Backend.Controllers.Mastodon.Schemas;
using Iceshrimp.Backend.Controllers.Mastodon.Schemas.Entities;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Backend.Core.Services;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

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
	ActivityPub.UserResolver userResolver,
	DriveService driveSvc,
	IOptionsSnapshot<Config.SecuritySection> config
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

	[HttpPatch("update_credentials")]
	[Authorize("write:accounts")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AccountEntity))]
	public async Task<IActionResult> UpdateUserCredentials([FromHybrid] AccountSchemas.AccountUpdateRequest request)
	{
		var user = HttpContext.GetUserOrFail();
		if (user.UserProfile == null)
			throw new Exception("User profile must not be null at this stage");

		if (request.DisplayName != null)
			user.DisplayName = !string.IsNullOrWhiteSpace(request.DisplayName) ? request.DisplayName : user.Username;
		if (request.Bio != null)
			user.UserProfile.Description = request.Bio;
		if (request.IsLocked.HasValue)
			user.IsLocked = request.IsLocked.Value;
		if (request.IsBot.HasValue)
			user.IsBot = request.IsBot.Value;
		if (request.IsExplorable.HasValue)
			user.IsExplorable = request.IsExplorable.Value;
		if (request.Source?.Sensitive.HasValue ?? false)
			user.UserProfile.AlwaysMarkNsfw = request.Source.Sensitive.Value;
		if (request.HideCollections.HasValue)
			user.UserProfile.FFVisibility = request.HideCollections.Value
				? UserProfile.UserProfileFFVisibility.Private
				: UserProfile.UserProfileFFVisibility.Public;

		if (user.UserSettings == null)
		{
			user.UserSettings = new UserSettings { User = user };
			db.Add(user.UserSettings);
		}

		if (request.Source?.Privacy != null)
			user.UserSettings.DefaultNoteVisibility = StatusEntity.DecodeVisibility(request.Source.Privacy);

		if (request.Fields?.Where(p => p is { Name: not null, Value: not null }).ToList() is { Count: > 0 } fields)
		{
			user.UserProfile.Fields =
				fields.Select(p => new UserProfile.Field
				      {
					      Name       = p.Name!,
					      Value      = p.Value!,
					      IsVerified = false
				      })
				      .ToArray();
		}

		var prevAvatarId = user.AvatarId;
		var prevBannerId = user.BannerId;

		if (request.Avatar != null)
		{
			var rq = new DriveFileCreationRequest
			{
				Filename    = request.Avatar.FileName,
				IsSensitive = false,
				MimeType    = request.Avatar.ContentType
			};
			var avatar = await driveSvc.StoreFile(request.Avatar.OpenReadStream(), user, rq);
			user.Avatar         = avatar;
			user.AvatarBlurhash = avatar.Blurhash;
			user.AvatarUrl      = avatar.Url;
		}

		if (request.Banner != null)
		{
			var rq = new DriveFileCreationRequest
			{
				Filename    = request.Banner.FileName,
				IsSensitive = false,
				MimeType    = request.Banner.ContentType
			};
			var banner = await driveSvc.StoreFile(request.Banner.OpenReadStream(), user, rq);
			user.Banner         = banner;
			user.BannerBlurhash = banner.Blurhash;
			user.BannerUrl      = banner.Url;
		}

		user = await userSvc.UpdateLocalUserAsync(user, prevAvatarId, prevBannerId);

		var res = await userRenderer.RenderAsync(user, user.UserProfile, source: true);
		return Ok(res);
	}

	[HttpDelete("/api/v1/profile/avatar")]
	[Authorize("write:accounts")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AccountEntity))]
	public async Task<IActionResult> DeleteUserAvatar()
	{
		var user = HttpContext.GetUserOrFail();
		if (user.AvatarId != null)
		{
			var id = user.AvatarId;

			user.AvatarId       = null;
			user.AvatarUrl      = null;
			user.AvatarBlurhash = null;

			db.Update(user);
			await db.SaveChangesAsync();
			await driveSvc.RemoveFile(id);
		}

		return await VerifyUserCredentials();
	}

	[HttpDelete("/api/v1/profile/header")]
	[Authorize("write:accounts")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AccountEntity))]
	public async Task<IActionResult> DeleteUserBanner()
	{
		var user = HttpContext.GetUserOrFail();
		if (user.BannerId != null)
		{
			var id = user.BannerId;

			user.BannerId       = null;
			user.BannerUrl      = null;
			user.BannerBlurhash = null;

			db.Update(user);
			await db.SaveChangesAsync();
			await driveSvc.RemoveFile(id);
		}

		return await VerifyUserCredentials();
	}

	[HttpGet("{id}")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AccountEntity))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(MastodonErrorResponse))]
	public async Task<IActionResult> GetUser(string id)
	{
		var localUser = HttpContext.GetUser();
		if (config.Value.PublicPreview == Enums.PublicPreview.Lockdown && localUser == null)
			throw GracefulException.Forbidden("Public preview is disabled on this instance");

		var user = await db.Users.IncludeCommonProperties().FirstOrDefaultAsync(p => p.Id == id) ??
		           throw GracefulException.RecordNotFound();

		if (config.Value.PublicPreview <= Enums.PublicPreview.Restricted && user.IsRemoteUser && localUser == null)
			throw GracefulException.Forbidden("Public preview is disabled on this instance");

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

		var res = RenderRelationship(followee);

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

		var res = RenderRelationship(followee);

		return Ok(res);
	}

	[HttpPost("{id}/mute")]
	[Authorize("write:mutes")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(RelationshipEntity))]
	public async Task<IActionResult> MuteUser(string id, [FromHybrid] AccountSchemas.AccountMuteRequest request)
	{
		var user = HttpContext.GetUserOrFail();
		if (user.Id == id)
			throw GracefulException.BadRequest("You cannot mute yourself");

		var mutee = await db.Users
		                    .Where(p => p.Id == id)
		                    .IncludeCommonProperties()
		                    .PrecomputeRelationshipData(user)
		                    .FirstOrDefaultAsync() ??
		            throw GracefulException.RecordNotFound();

		//TODO: handle notifications parameter
		DateTime? expiration = request.Duration == 0 ? null : DateTime.UtcNow + TimeSpan.FromSeconds(request.Duration);
		await userSvc.MuteUserAsync(user, mutee, expiration);

		var res = RenderRelationship(mutee);

		return Ok(res);
	}

	[HttpPost("{id}/unmute")]
	[Authorize("write:mutes")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(RelationshipEntity))]
	public async Task<IActionResult> UnmuteUser(string id)
	{
		var user = HttpContext.GetUserOrFail();
		if (user.Id == id)
			throw GracefulException.BadRequest("You cannot unmute yourself");

		var mutee = await db.Users
		                    .Where(p => p.Id == id)
		                    .IncludeCommonProperties()
		                    .PrecomputeRelationshipData(user)
		                    .FirstOrDefaultAsync() ??
		            throw GracefulException.RecordNotFound();

		await userSvc.UnmuteUserAsync(user, mutee);

		var res = RenderRelationship(mutee);

		return Ok(res);
	}

	[HttpPost("{id}/block")]
	[Authorize("write:blocks")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(RelationshipEntity))]
	public async Task<IActionResult> BlockUser(string id)
	{
		var user = HttpContext.GetUserOrFail();
		if (user.Id == id)
			throw GracefulException.BadRequest("You cannot block yourself");

		var blockee = await db.Users
		                      .Where(p => p.Id == id)
		                      .IncludeCommonProperties()
		                      .PrecomputeRelationshipData(user)
		                      .FirstOrDefaultAsync() ??
		              throw GracefulException.RecordNotFound();

		await userSvc.BlockUserAsync(user, blockee);

		var res = RenderRelationship(blockee);

		return Ok(res);
	}

	[HttpPost("{id}/unblock")]
	[Authorize("write:blocks")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(RelationshipEntity))]
	public async Task<IActionResult> UnblockUser(string id)
	{
		var user = HttpContext.GetUserOrFail();
		if (user.Id == id)
			throw GracefulException.BadRequest("You cannot unblock yourself");

		var blockee = await db.Users
		                      .Where(p => p.Id == id)
		                      .IncludeCommonProperties()
		                      .PrecomputeRelationshipData(user)
		                      .FirstOrDefaultAsync() ??
		              throw GracefulException.RecordNotFound();

		await userSvc.UnblockUserAsync(user, blockee);

		var res = RenderRelationship(blockee);

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

		var res = users.Select(RenderRelationship);

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
		                  .FilterByAccountStatusesRequest(request)
		                  .EnsureVisibleFor(user)
		                  .FilterHidden(user, db, except: id)
		                  .Paginate(query, ControllerContext)
		                  .PrecomputeVisibilities(user)
		                  .RenderAllForMastodonAsync(noteRenderer, user, Filter.FilterContext.Accounts);

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
		if (config.Value.PublicPreview == Enums.PublicPreview.Lockdown && user == null)
			throw GracefulException.Forbidden("Public preview is disabled on this instance");

		var account = await db.Users
		                      .Include(p => p.UserProfile)
		                      .FirstOrDefaultAsync(p => p.Id == id) ??
		              throw GracefulException.RecordNotFound();

		if (config.Value.PublicPreview <= Enums.PublicPreview.Restricted && account.IsRemoteUser && user == null)
			throw GracefulException.Forbidden("Public preview is disabled on this instance");

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
		if (config.Value.PublicPreview == Enums.PublicPreview.Lockdown && user == null)
			throw GracefulException.Forbidden("Public preview is disabled on this instance");

		var account = await db.Users
		                      .Include(p => p.UserProfile)
		                      .FirstOrDefaultAsync(p => p.Id == id) ??
		              throw GracefulException.RecordNotFound();

		if (config.Value.PublicPreview <= Enums.PublicPreview.Restricted && account.IsRemoteUser && user == null)
			throw GracefulException.Forbidden("Public preview is disabled on this instance");

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

	[HttpGet("{id}/featured_tags")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<object>))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(MastodonErrorResponse))]
	public async Task<IActionResult> GetUserFeaturedTags(string id)
	{
		_ = await db.Users
		            .Include(p => p.UserProfile)
		            .FirstOrDefaultAsync(p => p.Id == id) ??
		    throw GracefulException.RecordNotFound();

		var res = Array.Empty<object>();
		return Ok(res);
	}

	[HttpGet("/api/v1/follow_requests")]
	[Authorize("read:follows")]
	[LinkPagination(40, 80)]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<AccountEntity>))]
	public async Task<IActionResult> GetFollowRequests(MastodonPaginationQuery query)
	{
		var user = HttpContext.GetUserOrFail();
		var requests = await db.FollowRequests
		                       .Where(p => p.Followee == user)
		                       .IncludeCommonProperties()
		                       .Select(p => new EntityWrapper<User> { Id = p.Id, Entity = p.Follower })
		                       .Paginate(query, ControllerContext)
		                       .ToListAsync();

		HttpContext.SetPaginationData(requests);
		var res = await userRenderer.RenderManyAsync(requests.Select(p => p.Entity));

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
		var likes = await db.NoteLikes
		                    .Where(p => p.User == user)
		                    .IncludeCommonProperties()
		                    .Select(p => new EntityWrapper<Note>
		                    {
			                    Id = p.Id, Entity = p.Note.WithPrecomputedVisibilities(user)
		                    })
		                    .Paginate(query, ControllerContext)
		                    .ToListAsync();

		HttpContext.SetPaginationData(likes);
		var res = await noteRenderer.RenderManyAsync(likes.Select(p => p.Entity).EnforceRenoteReplyVisibility(), user);
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
		var bookmarks = await db.NoteBookmarks
		                        .Where(p => p.User == user)
		                        .IncludeCommonProperties()
		                        .Select(p => new EntityWrapper<Note>
		                        {
			                        Id = p.Id, Entity = p.Note.WithPrecomputedVisibilities(user)
		                        })
		                        .Paginate(query, ControllerContext)
		                        .ToListAsync();

		HttpContext.SetPaginationData(bookmarks);
		var res =
			await noteRenderer.RenderManyAsync(bookmarks.Select(p => p.Entity).EnforceRenoteReplyVisibility(), user);
		return Ok(res);
	}

	[HttpGet("/api/v1/blocks")]
	[Authorize("read:blocks")]
	[LinkPagination(40, 80)]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<AccountEntity>))]
	[SuppressMessage("ReSharper", "EntityFramework.UnsupportedServerSideFunctionCall", Justification = "Projectables")]
	public async Task<IActionResult> GetBlockedUsers(MastodonPaginationQuery pq)
	{
		var user = HttpContext.GetUserOrFail();
		var blocks = await db.Blockings
		                     .Include(p => p.Blockee.UserProfile)
		                     .Where(p => p.Blocker == user)
		                     .Select(p => new EntityWrapper<User> { Id = p.Id, Entity = p.Blockee })
		                     .Paginate(pq, ControllerContext)
		                     .ToListAsync();

		HttpContext.SetPaginationData(blocks);
		var res = await userRenderer.RenderManyAsync(blocks.Select(p => p.Entity));

		return Ok(res);
	}

	[HttpGet("/api/v1/mutes")]
	[Authorize("read:mutes")]
	[LinkPagination(40, 80)]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<AccountEntity>))]
	[SuppressMessage("ReSharper", "EntityFramework.UnsupportedServerSideFunctionCall", Justification = "Projectables")]
	public async Task<IActionResult> GetMutedUsers(MastodonPaginationQuery pq)
	{
		var user = HttpContext.GetUserOrFail();
		var mutes = await db.Mutings
		                    .Include(p => p.Mutee.UserProfile)
		                    .Where(p => p.Muter == user)
		                    .Select(p => new EntityWrapper<User> { Id = p.Id, Entity = p.Mutee })
		                    .Paginate(pq, ControllerContext)
		                    .ToListAsync();

		HttpContext.SetPaginationData(mutes);
		var res = await userRenderer.RenderManyAsync(mutes.Select(p => p.Entity));

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
		                           .Select(u => RenderRelationship(u))
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
		                           .Select(u => RenderRelationship(u))
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

	private static RelationshipEntity RenderRelationship(User u)
	{
		return new RelationshipEntity
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
		};
	}
}