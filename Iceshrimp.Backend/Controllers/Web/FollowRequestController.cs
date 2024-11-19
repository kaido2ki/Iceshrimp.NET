using System.Net;
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
	[ProducesResults(HttpStatusCode.OK)]
	public async Task<IEnumerable<FollowRequestResponse>> GetFollowRequests(PaginationQuery pq)
	{
		var user = HttpContext.GetUserOrFail();
		var requests = await db.FollowRequests
		                       .IncludeCommonProperties()
		                       .Where(p => p.Followee == user)
		                       .Paginate(pq, ControllerContext)
		                       .Select(p => new { p.Id, p.Follower })
		                       .ToListAsync();

		var users = await userRenderer.RenderManyAsync(requests.Select(p => p.Follower));
		return requests.Select(p => new FollowRequestResponse
		{
			Id = p.Id, User = users.First(u => u.Id == p.Follower.Id)
		});
	}

	[HttpPost("{id}/accept")]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.NotFound)]
	public async Task AcceptFollowRequest(string id)
	{
		var user = HttpContext.GetUserOrFail();
		var request = await db.FollowRequests
		                      .IncludeCommonProperties()
		                      .FirstOrDefaultAsync(p => p.Followee == user && p.Id == id) ??
		              throw GracefulException.NotFound("Follow request not found");

		await userSvc.AcceptFollowRequestAsync(request);
	}

	[HttpPost("{id}/reject")]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.NotFound)]
	public async Task RejectFollowRequest(string id)
	{
		var user = HttpContext.GetUserOrFail();
		var request = await db.FollowRequests
		                      .IncludeCommonProperties()
		                      .FirstOrDefaultAsync(p => p.Followee == user && p.Id == id) ??
		              throw GracefulException.NotFound("Follow request not found");

		await userSvc.RejectFollowRequestAsync(request);
	}
}