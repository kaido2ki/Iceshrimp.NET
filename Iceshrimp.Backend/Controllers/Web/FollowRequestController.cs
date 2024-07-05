using System.Net.Mime;
using Iceshrimp.Backend.Controllers.Shared.Attributes;
using Iceshrimp.Backend.Controllers.Shared.Schemas;
using Iceshrimp.Backend.Controllers.Web.Renderers;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Backend.Core.Services;
using Iceshrimp.Shared.Schemas.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Controllers.Web;

[ApiController]
[Authenticate]
[Authorize]
[EnableRateLimiting("sliding")]
[Route("/api/iceshrimp/follow_requests")]
[Produces(MediaTypeNames.Application.Json)]
public class FollowRequestController(
	DatabaseContext db,
	UserRenderer userRenderer,
	UserService userSvc
) : ControllerBase
{
	[HttpGet]
	[LinkPagination(20, 40)]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<FollowRequestResponse>))]
	public async Task<IActionResult> GetFollowRequests(PaginationQuery pq)
	{
		var user = HttpContext.GetUserOrFail();
		var requests = await db.FollowRequests
		                       .Where(p => p.Followee == user)
		                       .Paginate(pq, ControllerContext)
		                       .Select(p => new { p.Id, p.Follower })
		                       .ToListAsync();

		var users = await userRenderer.RenderMany(requests.Select(p => p.Follower));
		var res = requests.Select(p => new FollowRequestResponse
		{
			Id = p.Id, Entity = users.First(u => u.Id == p.Follower.Id)
		});
		return Ok(res.ToList());
	}

	[HttpPost("{id}/accept")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
	public async Task<IActionResult> AcceptFollowRequest(string id)
	{
		var user = HttpContext.GetUserOrFail();
		var request = await db.FollowRequests.FirstOrDefaultAsync(p => p.Followee == user && p.Id == id) ??
		              throw GracefulException.NotFound("Follow request not found");

		await userSvc.AcceptFollowRequestAsync(request);
		return Ok();
	}

	[HttpPost("{id}/reject")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
	public async Task<IActionResult> RejectFollowRequest(string id)
	{
		var user = HttpContext.GetUserOrFail();
		var request = await db.FollowRequests.FirstOrDefaultAsync(p => p.Followee == user && p.Id == id) ??
		              throw GracefulException.NotFound("Follow request not found");

		await userSvc.RejectFollowRequestAsync(request);
		return Ok();
	}
}