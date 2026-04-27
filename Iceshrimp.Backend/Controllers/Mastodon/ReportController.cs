using System.Net;
using System.Net.Mime;
using Iceshrimp.Backend.Controllers.Mastodon.Attributes;
using Iceshrimp.Backend.Controllers.Mastodon.Renderers;
using Iceshrimp.Backend.Controllers.Mastodon.Schemas;
using Iceshrimp.Backend.Controllers.Mastodon.Schemas.Entities;
using Iceshrimp.Backend.Controllers.Shared.Attributes;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Backend.Core.Services;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Controllers.Mastodon;

[Route("/api/v1/reports")]
[Authenticate]
[MastodonApiController]
[EnableCors("mastodon")]
[EnableRateLimiting("sliding")]
[Produces(MediaTypeNames.Application.Json)]
public class ReportController(ReportService reportSvc, DatabaseContext db, UserRenderer userRenderer) : ControllerBase
{
	[Authorize("write:reports")]
	[HttpPost]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.BadRequest, HttpStatusCode.NotFound)]
	public async Task<ReportEntity> FileReport([FromHybrid] ReportSchemas.FileReportRequest request)
	{
		var user = HttpContext.GetUserOrFail();
		if (request.Comment.Length > 2048)
			throw GracefulException.BadRequest("Comment length must not exceed 2048 characters");
		if (request.AccountId == user.Id)
			throw GracefulException.BadRequest("You cannot report yourself");

		var target = await db.Users.IncludeCommonProperties().FirstOrDefaultAsync(p => p.Id == request.AccountId)
		             ?? throw GracefulException.NotFound("Target user not found");

		var notes = await db.Notes.Where(p => request.StatusIds.Contains(p.Id)).ToListAsync();
		if (notes.Any(p => p.UserId != target.Id))
			throw GracefulException.BadRequest("Note author does not match target user");

		var rules = await db.Rules.Where(p => request.RuleIds.Contains(p.Id)).ToListAsync();

		var report        = await reportSvc.CreateReportAsync(user, target, notes, rules, request.Comment, request.Forward);
		var targetAccount = await userRenderer.RenderAsync(report.TargetUser, user);

		return new ReportEntity
		{
			Id            = report.Id,
			Category      = "other",
			Comment       = report.Comment,
			Forwarded     = report.Forwarded,
			ActionTaken   = report.Resolved,
			CreatedAt     = report.CreatedAt.ToStringIso8601Like(),
			TargetAccount = targetAccount,
			RuleIds       = request.RuleIds,
			StatusIds     = request.StatusIds,
			ActionTakenAt = null
		};
	}
}
