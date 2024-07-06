using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Mime;
using Iceshrimp.Backend.Controllers.Mastodon.Attributes;
using Iceshrimp.Backend.Controllers.Mastodon.Renderers;
using Iceshrimp.Backend.Controllers.Mastodon.Schemas;
using Iceshrimp.Backend.Controllers.Mastodon.Schemas.Entities;
using Iceshrimp.Backend.Controllers.Shared.Attributes;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Helpers;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Backend.Core.Services;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Controllers.Mastodon;

[MastodonApiController]
[Route("/api/v1/lists")]
[Authenticate]
[EnableRateLimiting("sliding")]
[EnableCors("mastodon")]
[Produces(MediaTypeNames.Application.Json)]
public class ListController(DatabaseContext db, UserRenderer userRenderer, EventService eventSvc) : ControllerBase
{
	[HttpGet]
	[Authorize("read:lists")]
	[ProducesResults(HttpStatusCode.OK)]
	public async Task<IEnumerable<ListEntity>> GetLists()
	{
		var user = HttpContext.GetUserOrFail();

		return await db.UserLists
		               .Where(p => p.User == user)
		               .Select(p => new ListEntity
		               {
			               Id        = p.Id,
			               Title     = p.Name,
			               Exclusive = p.HideFromHomeTl
		               })
		               .ToListAsync();
	}

	[HttpGet("{id}")]
	[Authorize("read:lists")]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.NotFound)]
	public async Task<ListEntity> GetList(string id)
	{
		var user = HttpContext.GetUserOrFail();

		return await db.UserLists
		               .Where(p => p.User == user && p.Id == id)
		               .Select(p => new ListEntity
		               {
			               Id        = p.Id,
			               Title     = p.Name,
			               Exclusive = p.HideFromHomeTl
		               })
		               .FirstOrDefaultAsync() ??
		       throw GracefulException.RecordNotFound();
	}

	[HttpPost]
	[Authorize("write:lists")]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.UnprocessableEntity)]
	public async Task<ListEntity> CreateList([FromHybrid] ListSchemas.ListCreationRequest request)
	{
		if (string.IsNullOrWhiteSpace(request.Title))
			throw GracefulException.UnprocessableEntity("Validation failed: Title can't be blank");

		var user = HttpContext.GetUserOrFail();
		var list = new UserList
		{
			Id             = IdHelpers.GenerateSlowflakeId(),
			CreatedAt      = DateTime.UtcNow,
			User           = user,
			Name           = request.Title,
			HideFromHomeTl = request.Exclusive
		};

		await db.AddAsync(list);
		await db.SaveChangesAsync();

		return new ListEntity
		{
			Id        = list.Id,
			Title     = list.Name,
			Exclusive = list.HideFromHomeTl
		};
	}

	[HttpPut("{id}")]
	[Authorize("write:lists")]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.NotFound, HttpStatusCode.UnprocessableEntity)]
	public async Task<ListEntity> UpdateList(string id, [FromHybrid] ListSchemas.ListUpdateRequest request)
	{
		var user = HttpContext.GetUserOrFail();
		var list = await db.UserLists
		                   .Where(p => p.User == user && p.Id == id)
		                   .FirstOrDefaultAsync() ??
		           throw GracefulException.RecordNotFound();

		if (request.Title != null && string.IsNullOrWhiteSpace(request.Title))
			throw GracefulException.UnprocessableEntity("Validation failed: Title can't be blank");

		list.Name           = request.Title ?? list.Name;
		list.HideFromHomeTl = request.Exclusive ?? list.HideFromHomeTl;

		db.Update(list);
		await db.SaveChangesAsync();

		return new ListEntity
		{
			Id        = list.Id,
			Title     = list.Name,
			Exclusive = list.HideFromHomeTl
		};
	}

	[HttpDelete("{id}")]
	[Authorize("write:lists")]
	[OverrideResultType<object>]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.NotFound)]
	public async Task<object> DeleteList(string id)
	{
		var user = HttpContext.GetUserOrFail();
		var list = await db.UserLists
		                   .Where(p => p.User == user && p.Id == id)
		                   .FirstOrDefaultAsync() ??
		           throw GracefulException.RecordNotFound();

		db.Remove(list);
		await db.SaveChangesAsync();
		eventSvc.RaiseListMembersUpdated(this, list);
		return new object();
	}

	[LinkPagination(40, 80)]
	[HttpGet("{id}/accounts")]
	[Authorize("read:lists")]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.NotFound)]
	public async Task<List<AccountEntity>> GetListMembers(string id, MastodonPaginationQuery pq)
	{
		var user = HttpContext.GetUserOrFail();
		var list = await db.UserLists
		                   .Where(p => p.User == user && p.Id == id)
		                   .FirstOrDefaultAsync() ??
		           throw GracefulException.RecordNotFound();

		return pq.Limit == 0
			? await db.UserListMembers
			          .Where(p => p.UserList == list)
			          .Include(p => p.User.UserProfile)
			          .Select(p => p.User)
			          .RenderAllForMastodonAsync(userRenderer)
			: await db.UserListMembers
			          .Where(p => p.UserList == list)
			          .Paginate(pq, ControllerContext)
			          .Include(p => p.User.UserProfile)
			          .Select(p => p.User)
			          .RenderAllForMastodonAsync(userRenderer);
	}

	[HttpPost("{id}/accounts")]
	[Authorize("write:lists")]
	[OverrideResultType<object>]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.NotFound, HttpStatusCode.UnprocessableEntity)]
	[SuppressMessage("ReSharper", "EntityFramework.UnsupportedServerSideFunctionCall", Justification = "Projectables")]
	public async Task<object> AddListMember(string id, [FromHybrid] ListSchemas.ListUpdateMembersRequest request)
	{
		var user = HttpContext.GetUserOrFail();
		var list = await db.UserLists
		                   .Where(p => p.User == user && p.Id == id)
		                   .FirstOrDefaultAsync() ??
		           throw GracefulException.RecordNotFound();

		var subjects = await db.Users.Where(p => request.AccountIds.Contains(p.Id) && p.IsFollowedBy(user))
		                       .Select(p => p.Id)
		                       .ToListAsync();

		if (subjects.Count == 0 || subjects.Count != request.AccountIds.Count)
			throw GracefulException.RecordNotFound();

		if (await db.UserListMembers.AnyAsync(p => subjects.Contains(p.UserId) && p.UserList == list))
			throw GracefulException.UnprocessableEntity("Validation failed: Account has already been taken");

		var memberships = subjects.Select(subject => new UserListMember
		{
			Id        = IdHelpers.GenerateSlowflakeId(),
			CreatedAt = DateTime.UtcNow,
			UserList  = list,
			UserId    = subject
		});

		await db.AddRangeAsync(memberships);
		await db.SaveChangesAsync();

		eventSvc.RaiseListMembersUpdated(this, list);

		return new object();
	}

	[HttpDelete("{id}/accounts")]
	[Authorize("write:lists")]
	[OverrideResultType<object>]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.NotFound)]
	public async Task<object> RemoveListMember(
		string id, [FromHybrid] ListSchemas.ListUpdateMembersRequest request
	)
	{
		var user = HttpContext.GetUserOrFail();
		var list = await db.UserLists
		                   .Where(p => p.User == user && p.Id == id)
		                   .FirstOrDefaultAsync() ??
		           throw GracefulException.RecordNotFound();

		await db.UserListMembers
		        .Where(p => p.UserList == list && request.AccountIds.Contains(p.UserId))
		        .ExecuteDeleteAsync();

		eventSvc.RaiseListMembersUpdated(this, list);

		return new object();
	}
}