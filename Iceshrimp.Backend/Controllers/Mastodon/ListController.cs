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
using Iceshrimp.Backend.Core.Helpers;
using Iceshrimp.Backend.Core.Middleware;
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
[ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(MastodonErrorResponse))]
[ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(MastodonErrorResponse))]
public class ListController(DatabaseContext db, UserRenderer userRenderer) : ControllerBase
{
	[HttpGet]
	[Authorize("read:lists")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<ListEntity>))]
	public async Task<IActionResult> GetLists()
	{
		var user = HttpContext.GetUserOrFail();

		var res = await db.UserLists
		                  .Where(p => p.User == user)
		                  .Select(p => new ListEntity { Id = p.Id, Title = p.Name, Exclusive = p.HideFromHomeTl })
		                  .ToListAsync();

		return Ok(res);
	}

	[HttpGet("{id}")]
	[Authorize("read:lists")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ListEntity))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(MastodonErrorResponse))]
	public async Task<IActionResult> GetList(string id)
	{
		var user = HttpContext.GetUserOrFail();

		var res = await db.UserLists
		                  .Where(p => p.User == user && p.Id == id)
		                  .Select(p => new ListEntity { Id = p.Id, Title = p.Name, Exclusive = p.HideFromHomeTl })
		                  .FirstOrDefaultAsync() ??
		          throw GracefulException.RecordNotFound();

		return Ok(res);
	}

	[HttpPost]
	[Authorize("write:lists")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ListEntity))]
	[ProducesResponseType(StatusCodes.Status422UnprocessableEntity, Type = typeof(MastodonErrorResponse))]
	public async Task<IActionResult> CreateList([FromHybrid] ListSchemas.ListCreationRequest request)
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

		var res = new ListEntity { Id = list.Id, Title = list.Name, Exclusive = list.HideFromHomeTl };
		return Ok(res);
	}

	[HttpPut("{id}")]
	[Authorize("write:lists")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ListEntity))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(MastodonErrorResponse))]
	[ProducesResponseType(StatusCodes.Status422UnprocessableEntity, Type = typeof(MastodonErrorResponse))]
	public async Task<IActionResult> UpdateList(string id, [FromHybrid] ListSchemas.ListCreationRequest request)
	{
		var user = HttpContext.GetUserOrFail();
		var list = await db.UserLists
		                   .Where(p => p.User == user && p.Id == id)
		                   .FirstOrDefaultAsync() ??
		           throw GracefulException.RecordNotFound();

		if (string.IsNullOrWhiteSpace(request.Title))
			throw GracefulException.UnprocessableEntity("Validation failed: Title can't be blank");


		list.Name           = request.Title;
		list.HideFromHomeTl = request.Exclusive;

		db.Update(list);
		await db.SaveChangesAsync();

		var res = new ListEntity { Id = list.Id, Title = list.Name, Exclusive = list.HideFromHomeTl };
		return Ok(res);
	}

	[HttpDelete("{id}")]
	[Authorize("write:lists")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(object))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(MastodonErrorResponse))]
	public async Task<IActionResult> DeleteList(string id)
	{
		var user = HttpContext.GetUserOrFail();
		var list = await db.UserLists
		                   .Where(p => p.User == user && p.Id == id)
		                   .FirstOrDefaultAsync() ??
		           throw GracefulException.RecordNotFound();

		db.Remove(list);
		await db.SaveChangesAsync();
		return Ok(new object());
	}

	[LinkPagination(40, 80)]
	[HttpGet("{id}/accounts")]
	[Authorize("read:lists")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<AccountEntity>))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(MastodonErrorResponse))]
	public async Task<IActionResult> GetListMembers(string id, MastodonPaginationQuery pq)
	{
		var user = HttpContext.GetUserOrFail();
		var list = await db.UserLists
		                   .Where(p => p.User == user && p.Id == id)
		                   .FirstOrDefaultAsync() ??
		           throw GracefulException.RecordNotFound();

		var res = pq.Limit == 0
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

		return Ok(res);
	}

	[HttpPost("{id}/accounts")]
	[Authorize("write:lists")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(object))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(MastodonErrorResponse))]
	[ProducesResponseType(StatusCodes.Status422UnprocessableEntity, Type = typeof(MastodonErrorResponse))]
	[SuppressMessage("ReSharper", "EntityFramework.UnsupportedServerSideFunctionCall", Justification = "Projectables")]
	public async Task<IActionResult> AddListMember(string id, [FromHybrid] ListSchemas.ListUpdateMembersRequest request)
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

		return Ok(new object());
	}

	[HttpDelete("{id}/accounts")]
	[Authorize("write:lists")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(object))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(MastodonErrorResponse))]
	public async Task<IActionResult> RemoveListMember(
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

		return Ok(new object());
	}
}