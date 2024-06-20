using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Backend.Core.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Pages;

public class QueueModel(DatabaseContext db, QueueService queueSvc) : PageModel
{
	public List<Job>      Jobs = [];
	public string?        Queue;
	public Job.JobStatus? Filter;
	public int?           TotalCount;
	public int?           QueuedCount;
	public int?           RunningCount;
	public int?           DelayedCount;
	public int?           PrevPage;
	public int?           NextPage;

	public async Task<IActionResult> OnGet(
		[FromRoute] string? queue, [FromRoute(Name = "pagination")] int? page, [FromRoute] string? status
	)
	{
		if (!Request.Cookies.TryGetValue("admin_session", out var cookie))
			return Redirect("/login");
		if (!await db.Sessions.AnyAsync(p => p.Token == cookie && p.Active && p.User.IsAdmin))
			return Redirect("/login");
		if (queue == null)
			return Page();
		if (!queueSvc.QueueNames.Contains(queue))
			throw GracefulException.BadRequest($"Unknown queue: {queue}");

		Queue = queue;

		if (page is null or < 1)
			page = 1;

		var query = db.Jobs.Where(p => p.Queue == queue);
		if (status is { Length: > 0 })
		{
			if (!Enum.TryParse<Job.JobStatus>(status, true, out var jobStatus))
				throw GracefulException.BadRequest($"Unknown status: {status}");
			query  = query.Where(p => p.Status == jobStatus);
			Filter = jobStatus;
		}

		Jobs = await query.OrderByDescending(p => p.Id)
		                  .Skip((page.Value - 1) * 50)
		                  .Take(50)
		                  .ToListAsync();
		
		if (Filter == null)
		{
			TotalCount   = await db.Jobs.CountAsync(p => p.Queue == queue);
			QueuedCount  = await db.Jobs.CountAsync(p => p.Queue == queue && p.Status == Job.JobStatus.Queued);
			RunningCount = await db.Jobs.CountAsync(p => p.Queue == queue && p.Status == Job.JobStatus.Running);
			DelayedCount = await db.Jobs.CountAsync(p => p.Queue == queue && p.Status == Job.JobStatus.Delayed);
		}
		else
		{
			TotalCount = await query.CountAsync();
		}

		if (Jobs.Count >= 50)
			NextPage = page + 1;
		if (page is > 1)
			PrevPage = page - 1;
		return Page();
	}
}