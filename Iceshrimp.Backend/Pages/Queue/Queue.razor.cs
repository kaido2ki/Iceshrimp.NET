using System.Collections.Immutable;
using Iceshrimp.Backend.Components.Helpers;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Backend.Core.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using static Iceshrimp.Backend.Core.Database.DatabaseContext;

namespace Iceshrimp.Backend.Pages.Queue;

public partial class Queue(DatabaseContext db, QueueService queueSvc, CacheService cache) : AdminComponentBase
{
    [Parameter] public string? Name       { get; set; }
    [Parameter] public int?    Pagination { get; set; }
    [Parameter] public string? Status     { get; set; }

    private int?                       DelayedCount  { get; set; }
    private int?                       QueuedCount   { get; set; }
    private int?                       RunningCount  { get; set; }
    private int?                       TotalCount    { get; set; }
    private Job.JobStatus?             Filter        { get; set; }
    private List<Job>                  Jobs          { get; set; } = [];
    private int?                       PrevPage      { get; set; }
    private int?                       NextPage      { get; set; }
    private List<QueueStatus>?         QueueStatuses { get; set; }
    private List<DelayedDeliverTarget> TopDelayed    { get; set; } = [];
    private long?                      Last          { get; set; }

    private NamedCache _cache = cache.GetNamedCache("admin:queue-dash");

    private static readonly ImmutableArray<string> ScheduledQueues = ["background-task", "backfill"];

    private class QueueStatus
    {
        public required string                                  Name      { get; init; }
        public required IReadOnlyDictionary<Job.JobStatus, int> JobCounts { get; init; }
    }

    protected override async Task OnInitializedAsync()
    {
        if (Name == null)
		{
			// Should be 15, but the table styles look more pleasing with even numbers
			Jobs = await db.Jobs.OrderByDescending(p => p.LastUpdatedAt).Take(16).ToListAsync();
			if (Context.Request.Query.TryGetValue("last", out var last) && long.TryParse(last, out var parsed))
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
							                        job.Queue == queueName
							                        && job.Status
							                        == Job.JobStatus.Queued)
					                        },
					                        {
						                        Job.JobStatus.Delayed, db.Jobs.Count(job =>
							                        job.Queue == queueName && job.Status == Job.JobStatus.Delayed)
					                        },
					                        {
						                        Job.JobStatus.Running, db.Jobs.Count(job =>
							                        job.Queue == queueName && job.Status == Job.JobStatus.Running)
					                        },
					                        {
						                        Job.JobStatus.Completed, db.Jobs.Count(job =>
							                        job.Queue == queueName
							                        && job.Status == Job.JobStatus.Completed)
					                        },
					                        {
						                        Job.JobStatus.Failed, db.Jobs.Count(job =>
							                        job.Queue == queueName
							                        && job.Status
							                        == Job.JobStatus.Failed)
					                        }
				                        }.AsReadOnly()
			                        })
			                        .ToListAsync();

			TopDelayed = await _cache.FetchAsync("top-delayed", TimeSpan.FromSeconds(60),
			                                     () => db.GetDelayedDeliverTargets().ToListAsync());

			return;
		}

		if (!queueSvc.QueueNames.Contains(Name))
			throw GracefulException.BadRequest($"Unknown queue: {Name}");

		if (Pagination is null or < 1)
			Pagination = 1;

		var query = db.Jobs.Where(p => p.Queue == Name);
		if (Status is { Length: > 0 })
		{
			if (!Enum.TryParse<Job.JobStatus>(Status, true, out var jobStatus))
				throw GracefulException.BadRequest($"Unknown status: {Status}");
			query  = query.Where(p => p.Status == jobStatus);
			Filter = jobStatus;
		}

		Jobs = await query.OrderByDescending(p => p.Id)
		                  .Skip((Pagination.Value - 1) * 50)
		                  .Take(50)
		                  .ToListAsync();

		if (Filter == null)
		{
			TotalCount   = await db.Jobs.CountAsync(p => p.Queue == Name);
			QueuedCount  = await db.Jobs.CountAsync(p => p.Queue == Name && p.Status == Job.JobStatus.Queued);
			RunningCount = await db.Jobs.CountAsync(p => p.Queue == Name && p.Status == Job.JobStatus.Running);
			DelayedCount = await db.Jobs.CountAsync(p => p.Queue == Name && p.Status == Job.JobStatus.Delayed);
		}
		else
		{
			TotalCount = await query.CountAsync();
		}

		if (Jobs.Count >= 50)
			NextPage = Pagination + 1;
		if (Pagination is > 1)
			PrevPage = Pagination - 1;
    }
}

