using System.Diagnostics.CodeAnalysis;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Core.Tasks;

[SuppressMessage("ReSharper", "UnusedType.Global", Justification = "Instantiated at runtime by CronService")]
public class JobCleanupTask : ICronTask
{
	public async Task Invoke(IServiceProvider provider)
	{
		var db        = provider.GetRequiredService<DatabaseContext>();
		var queue     = provider.GetRequiredService<QueueService>();
		var retention = provider.GetRequiredService<IOptionsSnapshot<Config.JobRetentionSection>>();

		foreach (var name in queue.QueueNames)
		{
			await db.Jobs.Where(p => p.Queue == name && p.Status == Job.JobStatus.Completed)
			        .OrderByDescending(p => p.FinishedAt)
			        .Skip(retention.Value.Completed)
			        .ExecuteDeleteAsync();

			await db.Jobs.Where(p => p.Queue == name && p.Status == Job.JobStatus.Failed)
			        .OrderByDescending(p => p.FinishedAt)
			        .Skip(retention.Value.Failed)
			        .ExecuteDeleteAsync();
		}
	}

	public CronTaskType Type    => CronTaskType.Interval;
	public TimeSpan     Trigger => TimeSpan.FromMinutes(15);
}