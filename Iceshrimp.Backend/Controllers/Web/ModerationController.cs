using System.Net;
using Iceshrimp.Backend.Controllers.Shared.Attributes;
using Iceshrimp.Backend.Controllers.Shared.Schemas;
using Iceshrimp.Backend.Controllers.Web.Renderers;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Backend.Core.Services;
using Iceshrimp.Shared.Schemas.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Controllers.Web;

[Authenticate]
[Authorize("role:moderator")]
[ApiController]
[Route("/api/iceshrimp/moderation")]
public class ModerationController(
	DatabaseContext db,
	NoteService noteSvc,
	UserService userSvc,
	ReportRenderer reportRenderer,
	ReportService reportSvc
) : ControllerBase
{
	[HttpPost("notes/{id}/delete")]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.NotFound)]
	public async Task DeleteNote(string id)
	{
		var note = await db.Notes.IncludeCommonProperties().FirstOrDefaultAsync(p => p.Id == id)
		           ?? throw GracefulException.NotFound("Note not found");

		await noteSvc.DeleteNoteAsync(note);
	}

	[HttpPost("users/{id}/suspend")]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.NotFound)]
	public async Task SuspendUser(string id)
	{
		var user = await db.Users.IncludeCommonProperties().FirstOrDefaultAsync(p => p.Id == id && !p.IsSystemUser)
		           ?? throw GracefulException.NotFound("User not found");

		if (user == HttpContext.GetUserOrFail())
			throw GracefulException.BadRequest("You cannot suspend yourself.");

		await userSvc.SuspendUserAsync(user);
	}

	[HttpPost("users/{id}/unsuspend")]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.NotFound)]
	public async Task UnsuspendUser(string id)
	{
		var user = await db.Users.IncludeCommonProperties().FirstOrDefaultAsync(p => p.Id == id && !p.IsSystemUser)
		           ?? throw GracefulException.NotFound("User not found");

		if (user == HttpContext.GetUserOrFail())
			throw GracefulException.BadRequest("You cannot unsuspend yourself.");

		await userSvc.UnsuspendUserAsync(user);
	}

	[HttpPost("users/{id}/delete")]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.NotFound)]
	public async Task DeleteUser(string id)
	{
		var user = await db.Users.IncludeCommonProperties().FirstOrDefaultAsync(p => p.Id == id && !p.IsSystemUser)
		           ?? throw GracefulException.NotFound("User not found");

		if (user == HttpContext.GetUserOrFail())
			throw GracefulException.BadRequest("You cannot delete yourself.");

		await userSvc.DeleteUserAsync(user);
	}

	[HttpPost("users/{id}/purge")]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.NotFound)]
	public async Task PurgeUser(string id)
	{
		var user = await db.Users.IncludeCommonProperties().FirstOrDefaultAsync(p => p.Id == id && !p.IsSystemUser)
		           ?? throw GracefulException.NotFound("User not found");

		await userSvc.PurgeUserAsync(user);
	}

	[HttpGet("reports")]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.BadRequest)]
	[LinkPagination(20, 40)]
	public async Task<IEnumerable<ReportResponse>> GetReports(PaginationQuery pq, bool resolved = false)
	{
		var reports = await db.Reports
		                      .IncludeCommonProperties()
		                      .Where(p => p.Resolved == resolved)
		                      .Paginate(pq, ControllerContext)
		                      .ToListAsync();

		return await reportRenderer.RenderManyAsync(reports);
	}

	[HttpPost("reports/{id}/resolve")]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.NotFound)]
	public async Task ResolveReport(string id)
	{
		var report = await db.Reports.FirstOrDefaultAsync(p => p.Id == id)
		             ?? throw GracefulException.NotFound("Report not found");

		report.Resolved = true;
		await db.SaveChangesAsync();
	}

	[HttpPost("reports/{id}/forward")]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.BadRequest, HttpStatusCode.NotFound)]
	public async Task ForwardReport(string id, [FromBody] NoteReportRequest? request)
	{
		var report = await db.Reports
		                     .Include(p => p.TargetUser)
		                     .Include(p => p.Notes)
		                     .FirstOrDefaultAsync(p => p.Id == id)
		             ?? throw GracefulException.NotFound("Report not found");

		if (report.TargetUserHost == null)
			throw GracefulException.BadRequest("Cannot forward report to local instance");
		if (report.Forwarded)
			return;

		await reportSvc.ForwardReportAsync(report, request?.Comment);

		report.Forwarded = true;
		await db.SaveChangesAsync();
	}
}
