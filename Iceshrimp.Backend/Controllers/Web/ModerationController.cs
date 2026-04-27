using System.Net;
using Iceshrimp.Backend.Controllers.Shared.Attributes;
using Iceshrimp.Backend.Controllers.Shared.Schemas;
using Iceshrimp.Backend.Controllers.Web.Renderers;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Backend.Core.Services;
using Iceshrimp.Shared.Schemas.Web;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Controllers.Web;

/// <summary>
/// <para>Operations for performing actions required for instance moderation.</para>
/// <para>Requires role: <b>Moderator</b></para>
/// </summary>
[Authenticate]
[Authorize("role:moderator")]
[ApiController]
[Route("/api/iceshrimp/moderation")]
[EnableCors("iceshrimp")]
public class ModerationController(
	DatabaseContext db,
	NoteService noteSvc,
	UserService userSvc,
	ReportRenderer reportRenderer,
	ReportService reportSvc
) : ControllerBase
{
	/// <summary>
	/// Delete note
	/// </summary>
	/// <remarks>Delete a note from any user and removes it from federation where possible.</remarks>
	/// <param name="id">The note's ID</param>
	[HttpPost("notes/{id}/delete")]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.NotFound)]
	public async Task DeleteNote(string id)
	{
		var note = await db.Notes.IncludeCommonProperties().FirstOrDefaultAsync(p => p.Id == id)
		           ?? throw GracefulException.NotFound("Note not found");

		await noteSvc.DeleteNoteAsync(note);
	}

	/// <summary>
	/// Suspend user
	/// </summary>
	/// <param name="id">The user's ID</param>
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

	/// <summary>
	/// Unsuspend user
	/// </summary>
	/// <param name="id">The user's ID</param>
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

	/// <summary>
	/// Delete user
	/// </summary>
	/// <remarks><b>This action cannot be undone.</b></remarks>
	/// <param name="id">The user's ID</param>
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

	/// <summary>
	/// Purge user
	/// </summary>
	/// <remarks>
	/// <para>Delete all Drive files and notes from a user and removes them from federation where possible.</para>
	/// <para><b>This action cannot be undone.</b></para>
	/// </remarks>
	/// <param name="id">The user's ID.</param>
	[HttpPost("users/{id}/purge")]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.NotFound)]
	public async Task PurgeUser(string id)
	{
		var user = await db.Users.IncludeCommonProperties().FirstOrDefaultAsync(p => p.Id == id && !p.IsSystemUser)
		           ?? throw GracefulException.NotFound("User not found");

		await userSvc.PurgeUserAsync(user);
	}

	/// <summary>
	/// List reports
	/// </summary>
	/// <remarks>Returns a paginated list of user reports.</remarks>
	/// <param name="pq">Pagination query</param>
	/// <param name="resolved">Include resolved reports</param>
	/// <response code="200">Paginated list of user reports</response>
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

	/// <summary>
	/// Get report
	/// </summary>
	/// <param name="id">The report's ID</param>
	/// <response code="200">User report</response>
	[HttpGet("reports/{id}")]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.NotFound)]
	public async Task<ReportResponse> GetReport(string id)
	{
		var report = await db.Reports
		                     .IncludeCommonProperties()
		                     .FirstOrDefaultAsync(p => p.Id == id)
		             ?? throw GracefulException.RecordNotFound();

		return await reportRenderer.RenderOneAsync(report);
	}

	/// <summary>
	/// Mark report as resolved
	/// </summary>
	/// <param name="id">The report's ID</param>
	[HttpPost("reports/{id}/resolve")]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.NotFound)]
	public async Task ResolveReport(string id)
	{
		var user = HttpContext.GetUserOrFail();
		var report = await db.Reports.FirstOrDefaultAsync(p => p.Id == id)
		             ?? throw GracefulException.NotFound("Report not found");

		report.Assignee = user;
		report.Resolved = true;
		await db.SaveChangesAsync();
	}

	/// <summary>
	/// Forward report
	/// </summary>
	/// <remarks>Forward report of a <b>remote</b> user to the staff of their instance.</remarks>
	/// <param name="id">The report's ID</param>
	/// <param name="request">Forward report request</param>
	/// <response code="400">Cannot forward report to local instance</response>
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

	/// <summary>
	///	Delete report
	/// </summary>
	/// <param name="id">The report's ID</param>
	[HttpPost("reports/{id}/delete")]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.BadRequest, HttpStatusCode.NotFound)]
	public async Task DeleteReport(string id)
	{
		var report = await db.Reports.FirstOrDefaultAsync(p => p.Id == id)
			?? throw GracefulException.NotFound("Report not found");

		db.Remove(report);
		await db.SaveChangesAsync();
	}

	/// <summary>
	/// Refetch emoji
	/// </summary>
	/// <remarks>Attempt to refetch a <b>remote</b> emoji if it is broken. This is not supported on most instance software.</remarks>
	/// <param name="id">The emoji's ID</param>
	/// <param name="emojiSvc">DI</param>
	/// <param name="instance">DI</param>
	[HttpPost("emoji/{id}/refetch")]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.NotFound)]
	public async Task<ModerationSchemas.EmojiRefetchResponse> RefetchEmoji(
		string id, [FromServices] EmojiService emojiSvc, [FromServices] IOptions<Config.InstanceSection> instance
	)
	{
		var emoji = await db.Emojis.FirstOrDefaultAsync(p => p.Id == id)
		            ?? throw GracefulException.NotFound("Emoji not found");

		var (success, updatedEmoji) = await emojiSvc.UpdateRemoteEmojiAsync(emoji);

		var emojiRes = new EmojiResponse
		{
			Id        = updatedEmoji.Id,
			Name      = updatedEmoji.Name,
			Uri       = updatedEmoji.Uri,
			Tags      = updatedEmoji.Tags,
			Category  = updatedEmoji.Host,
			PublicUrl = updatedEmoji.GetAccessUrl(instance.Value),
			License   = updatedEmoji.License,
			Sensitive = updatedEmoji.Sensitive
		};

		return new ModerationSchemas.EmojiRefetchResponse { Success = success, Emoji = emojiRes };
	}
}
