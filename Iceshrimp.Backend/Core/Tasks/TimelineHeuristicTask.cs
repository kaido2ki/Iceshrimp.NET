using System.Diagnostics.CodeAnalysis;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Services;

namespace Iceshrimp.Backend.Core.Tasks;

[SuppressMessage("ReSharper", "UnusedType.Global", Justification = "Instantiated at runtime by CronService")]
public class TimelineHeuristicTask : ICronTask
{
	public async Task InvokeAsync(IServiceProvider provider)
	{
		var db           = provider.GetRequiredService<DatabaseContext>();
		var cache        = provider.GetRequiredService<CacheService>();
		var logger       = provider.GetRequiredService<ILogger<TimelineHeuristicTask>>();
		var activeCutoff = DateTime.UtcNow - TimeSpan.FromHours(24);

		logger.LogDebug("Updating timeline heuristic for recently active users");

		var users = db.Users
		              .Where(p => p.IsLocalUser && p.LastActiveDate > activeCutoff)
		              .NeedsTimelineHeuristicUpdate(db, TimeSpan.FromHours(2))
		              .AsChunkedAsyncEnumerable(10, p => p.Id);

		await foreach (var user in users)
		{
			logger.LogDebug("Updating timeline heuristic for user {userId}...", user.Id);
			await QueryableTimelineExtensions.GetHeuristicAsync(user, db, cache, forceUpdate: true);
		}

		logger.LogDebug("Finished updating timeline heuristic for active users.");
	}

	public TimeSpan     Trigger => TimeSpan.FromHours(1);
	public CronTaskType Type    => CronTaskType.Interval;
}
