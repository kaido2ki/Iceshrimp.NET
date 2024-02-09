using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ProtoBuf;
using StackExchange.Redis;

namespace Iceshrimp.Backend.Core.Queues;

public abstract class BackgroundTaskQueue {
	public static JobQueue<BackgroundTaskJob> Create(IConnectionMultiplexer redis, string prefix) {
		return new JobQueue<BackgroundTaskJob>("background-task", BackgroundTaskQueueProcessorDelegateAsync, 4, redis,
		                                       prefix);
	}

	private static async Task BackgroundTaskQueueProcessorDelegateAsync(
		BackgroundTaskJob job,
		IServiceProvider scope,
		CancellationToken token
	) {
		if (job is DriveFileDeleteJob driveFileDeleteJob) {
			await ProcessDriveFileDelete(driveFileDeleteJob, scope, token);
		}
	}

	private static async Task ProcessDriveFileDelete(
		DriveFileDeleteJob job,
		IServiceProvider scope,
		CancellationToken token
	) {
		var db = scope.GetRequiredService<DatabaseContext>();
		var usedAsAvatarOrBanner =
			await db.Users.AnyAsync(p => p.AvatarId == job.DriveFileId ||
			                             p.BannerId == job.DriveFileId, cancellationToken: token);

		var usedInNote = await db.Notes.AnyAsync(p => p.FileIds.Contains(job.DriveFileId), cancellationToken: token);

		if (!usedAsAvatarOrBanner && !usedInNote) {
			var file = await db.DriveFiles.FirstOrDefaultAsync(p => p.Id == job.DriveFileId, cancellationToken: token);
			if (file != null) {
				string?[] paths = [file.AccessKey, file.ThumbnailAccessKey, file.WebpublicAccessKey];

				if (file.StoredInternal) {
					var pathBase = scope.GetRequiredService<IOptions<Config.StorageSection>>().Value.Local?.Path
					               ?? throw new Exception("Cannot delete locally stored file: pathBase is null");

					paths.Where(p => p != null)
					     .Select(p => Path.Combine(pathBase, p!))
					     .Where(File.Exists).ToList()
					     .ForEach(File.Delete);
				}
				else {
					var storageSvc = scope.GetRequiredService<ObjectStorageService>();
					await storageSvc.RemoveFilesAsync(paths.Where(p => p != null).Select(p => p!).ToArray());
				}

				db.Remove(file);
				await db.SaveChangesAsync(token);
			}
		}
	}
}

[ProtoContract]
[ProtoInclude(100, typeof(DriveFileDeleteJob))]
public class BackgroundTaskJob : Job;

[ProtoContract]
public class DriveFileDeleteJob : BackgroundTaskJob {
	[ProtoMember(1)] public required string DriveFileId;
}