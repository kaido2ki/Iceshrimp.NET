using System.Net;
using System.Net.Mime;
using Iceshrimp.Backend.Controllers.Shared.Attributes;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Middleware;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using static Iceshrimp.Shared.Schemas.Web.SessionSchemas;

namespace Iceshrimp.Backend.Controllers.Web;

[ApiController]
[Authenticate]
[Authorize]
[Tags("Session")]
[EnableRateLimiting("sliding")]
[Produces(MediaTypeNames.Application.Json)]
[Route("/api/iceshrimp/sessions")]
public class SessionController(DatabaseContext db) : ControllerBase
{
	[HttpGet]
	[ProducesResults(HttpStatusCode.OK)]
	public async Task<List<SessionResponse>> GetSessions(int page = 0)
	{
		const int pageSize  = 20;
		var       currentId = HttpContext.GetSessionOrFail().Id;

		return await db.Sessions
		               .Where(p => p.User == HttpContext.GetUserOrFail())
		               .OrderByDescending(p => p.LastActiveDate ?? p.CreatedAt)
		               .Skip(page * pageSize)
		               .Take(pageSize)
		               .Select(p => new SessionResponse
		               {
			               Id         = p.Id,
			               Current    = p.Id == currentId,
			               Active     = p.Active,
			               CreatedAt  = p.CreatedAt,
			               LastActive = p.LastActiveDate
		               })
		               .ToListAsync();
	}

	[HttpDelete("{id}")]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.BadRequest, HttpStatusCode.NotFound)]
	public async Task TerminateSession(string id)
	{
		var user = HttpContext.GetUserOrFail();
		var session = await db.Sessions.FirstOrDefaultAsync(p => p.Id == id && p.User == user)
		              ?? throw GracefulException.NotFound("Session not found");

		if (session.Id == HttpContext.GetSessionOrFail().Id)
			throw GracefulException.BadRequest("Refusing to terminate current session");

		db.Remove(session);
		await db.SaveChangesAsync();
	}
}
