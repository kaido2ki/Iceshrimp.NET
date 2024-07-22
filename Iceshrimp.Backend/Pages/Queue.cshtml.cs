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
	public int?               DelayedCount;
	public Job.JobStatus?     Filter;
	public List<Job>          Jobs = [];
	public int?               NextPage;
	public int?               PrevPage;
	public string?            Queue;
	public int?               QueuedCount;
	public int?               RunningCount;
	public int?               TotalCount;
	public List<QueueStatus>? QueueStatuses;
	public long?              Last;

	public async Task<IActionResult> OnGet(
		[FromRoute] string? queue, [FromRoute(Name = "pagination")] int? page, [FromRoute] string? status
	)
	{
		if (!Request.Cookies.TryGetValue("admin_session", out var cookie))
			return Redirect("/login");
		if (!await db.Sessions.AnyAsync(p => p.Token == cookie && p.Active && p.User.IsAdmin))
			return Redirect("/login");

		if (queue == null)
		{
			Jobs = await db.Jobs.OrderByDescending(p => p.LastUpdatedAt).Take(20).ToListAsync();
			if (Request.Query.TryGetValue("last", out var last) && long.TryParse(last, out var parsed))
				Last = parsed;

			//TODO: write an expression generator for the job count calculation
			QueueStatuses = await db.Jobs
			                        .GroupBy(job => job.Queue)
			                        .OrderBy(p => p.Key)
			                        .Select(p => p.Key)
			                        .Select(queueName => new QueueStatus
			                        {
				                        Name = queueName,
				                        JobCounts = new Dictionary<Job.JobStatus, int>
				                        {
					                        {
						                        Job.JobStatus.Queued, db.Jobs.Count(job =>
							                        job.Queue == queueName &&
							                        job.Status ==
							                        Job.JobStatus.Queued)
					                        },
					                        {
						                        Job.JobStatus.Delayed, db.Jobs.Count(job =>
							                        job.Queue == queueName &&
							                        job.Status == Job.JobStatus.Delayed)
					                        },
					                        {
						                        Job.JobStatus.Running, db.Jobs.Count(job =>
							                        job.Queue == queueName &&
							                        job.Status == Job.JobStatus.Running)
					                        },
					                        {
						                        Job.JobStatus.Completed, db.Jobs.Count(job =>
							                        job.Queue == queueName &&
							                        job.Status == Job.JobStatus.Completed)
					                        },
					                        {
						                        Job.JobStatus.Failed, db.Jobs.Count(job =>
							                        job.Queue == queueName &&
							                        job.Status ==
							                        Job.JobStatus.Failed)
					                        }
				                        }.AsReadOnly()
			                        })
			                        .ToListAsync();

			return Page();
		}

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

	public class QueueStatus
	{
		public required string                                  Name      { get; init; }
		public required IReadOnlyDictionary<Job.JobStatus, int> JobCounts { get; init; }
	}
}