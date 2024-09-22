using System.Diagnostics.CodeAnalysis;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Queues;
using Iceshrimp.Backend.Core.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Core.Tasks;

[SuppressMessage("ReSharper", "UnusedType.Global", Justification = "Instantiated at runtime by CronService")]
public class MediaCleanupTask : ICronTask
{
	public async Task Invoke(IServiceProvider provider)
	{
		var config = provider.GetRequiredService<IOptionsSnapshot<Config.StorageSection>>().Value;
		if (config.MediaRetentionTimeSpan == TimeSpan.MaxValue) return;

		var logger = provider.GetRequiredService<ILogger<MediaCleanupTask>>();
		logger.LogInformation("Starting media cleanup task...");

		var db           = provider.GetRequiredService<DatabaseContext>();
		var queueService = provider.GetRequiredService<QueueService>();

		var cutoff = DateTime.UtcNow - (config.MediaRetentionTimeSpan ?? TimeSpan.Zero);

		var query = db.DriveFiles.Where(p => !p.IsLink && p.UserHost != null && p.CreatedAt < cutoff);

		if (!config.CleanAvatars) query = query.Where(p => !db.Users.Any(u => u.AvatarId == p.Id));
		if (!config.CleanBanners) query = query.Where(p => !db.Users.Any(u => u.BannerId == p.Id));

		var fileIds = query.Select(p => p.Id);
		var cnt     = await fileIds.CountAsync();

		logger.LogInformation("Expiring {count} files...", cnt);
		await foreach (var fileId in fileIds.AsChunkedAsyncEnumerable(50))
		{
			await queueService.BackgroundTaskQueue.EnqueueAsync(new DriveFileDeleteJobData
			{
				DriveFileId = fileId, Expire = true
			});
		}

		logger.LogInformation("Successfully queued {count} media cleanup jobs.", cnt);
	}

	// Midnight
	public TimeSpan     Trigger => TimeSpan.Zero;
	public CronTaskType Type    => CronTaskType.Daily;
}