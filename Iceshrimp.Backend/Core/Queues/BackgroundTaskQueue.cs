using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ProtoBuf;
using StackExchange.Redis;

namespace Iceshrimp.Backend.Core.Queues;

public abstract class BackgroundTaskQueue
{
	public static JobQueue<BackgroundTaskJob> Create(IConnectionMultiplexer redis, string prefix)
	{
		return new JobQueue<BackgroundTaskJob>("background-task", BackgroundTaskQueueProcessorDelegateAsync, 4, redis,
		                                       prefix);
	}

	private static async Task BackgroundTaskQueueProcessorDelegateAsync(
		BackgroundTaskJob job,
		IServiceProvider scope,
		CancellationToken token
	)
	{
		if (job is DriveFileDeleteJob driveFileDeleteJob)
		{
			if (driveFileDeleteJob.Expire)
				await ProcessDriveFileExpire(driveFileDeleteJob, scope, token);
			else
				await ProcessDriveFileDelete(driveFileDeleteJob, scope, token);
		}
		else if (job is PollExpiryJob pollExpiryJob)
		{
			await ProcessPollExpiry(pollExpiryJob, scope, token);
		}
	}

	private static async Task ProcessDriveFileDelete(
		DriveFileDeleteJob job,
		IServiceProvider scope,
		CancellationToken token
	)
	{
		var db = scope.GetRequiredService<DatabaseContext>();
		var usedAsAvatarOrBanner =
			await db.Users.AnyAsync(p => p.AvatarId == job.DriveFileId ||
			                             p.BannerId == job.DriveFileId, token);

		var usedInNote = await db.Notes.AnyAsync(p => p.FileIds.Contains(job.DriveFileId), token);

		if (!usedAsAvatarOrBanner && !usedInNote)
		{
			var file = await db.DriveFiles.FirstOrDefaultAsync(p => p.Id == job.DriveFileId, token);
			if (file != null)
			{
				string?[] paths = [file.AccessKey, file.ThumbnailAccessKey, file.WebpublicAccessKey];

				if (file.StoredInternal)
				{
					var pathBase = scope.GetRequiredService<IOptions<Config.StorageSection>>().Value.Local?.Path ??
					               throw new Exception("Cannot delete locally stored file: pathBase is null");

					paths.Where(p => p != null)
					     .Select(p => Path.Combine(pathBase, p!))
					     .Where(File.Exists)
					     .ToList()
					     .ForEach(File.Delete);
				}
				else
				{
					var storageSvc = scope.GetRequiredService<ObjectStorageService>();
					await storageSvc.RemoveFilesAsync(paths.Where(p => p != null).Select(p => p!).ToArray());
				}

				db.Remove(file);
				await db.SaveChangesAsync(token);
			}
		}
	}

	private static async Task ProcessDriveFileExpire(
		DriveFileDeleteJob job,
		IServiceProvider scope,
		CancellationToken token
	)
	{
		var db     = scope.GetRequiredService<DatabaseContext>();
		var logger = scope.GetRequiredService<ILogger<BackgroundTaskQueue>>();
		logger.LogDebug("Expiring file {id}...", job.DriveFileId);

		var file = await db.DriveFiles.FirstOrDefaultAsync(p => p.Id == job.DriveFileId, token);
		if (file is not { UserHost: not null, Uri: not null }) return;

		file.IsLink             = true;
		file.Url                = file.Uri;
		file.ThumbnailUrl       = null;
		file.WebpublicUrl       = null;
		file.ThumbnailAccessKey = null;
		file.WebpublicAccessKey = null;
		file.StoredInternal     = false;

		await db.Users.Where(p => p.AvatarId == file.Id)
		        .ExecuteUpdateAsync(p => p.SetProperty(u => u.AvatarUrl, file.Uri), token);
		await db.Users.Where(p => p.BannerId == file.Id)
		        .ExecuteUpdateAsync(p => p.SetProperty(u => u.BannerUrl, file.Uri), token);
		await db.SaveChangesAsync(token);

		if (file.AccessKey == null) return;

		string?[] paths = [file.AccessKey, file.ThumbnailAccessKey, file.WebpublicAccessKey];
		if (!await db.DriveFiles.AnyAsync(p => p.Id != file.Id && p.AccessKey == file.AccessKey,
		                                  token))
		{
			if (file.StoredInternal)
			{
				var pathBase = scope.GetRequiredService<IOptions<Config.StorageSection>>().Value.Local?.Path ??
				               throw new Exception("Cannot delete locally stored file: pathBase is null");

				paths.Where(p => p != null)
				     .Select(p => Path.Combine(pathBase, p!))
				     .Where(File.Exists)
				     .ToList()
				     .ForEach(File.Delete);
			}
			else
			{
				var storageSvc = scope.GetRequiredService<ObjectStorageService>();
				await storageSvc.RemoveFilesAsync(paths.Where(p => p != null).Select(p => p!).ToArray());
			}
		}
	}

	private static async Task ProcessPollExpiry(
		PollExpiryJob job,
		IServiceProvider scope,
		CancellationToken token
	)
	{
		var db   = scope.GetRequiredService<DatabaseContext>();
		var poll = await db.Polls.FirstOrDefaultAsync(p => p.NoteId == job.NoteId, cancellationToken: token);
		if (poll == null) return;
		if (poll.ExpiresAt > DateTime.UtcNow + TimeSpan.FromMinutes(5)) return;
		var note = await db.Notes.IncludeCommonProperties().FirstOrDefaultAsync(p => p.Id == poll.NoteId, cancellationToken: token);
		if (note == null) return;
		//TODO: try to update poll before completing it
		var notificationSvc = scope.GetRequiredService<NotificationService>();
		await notificationSvc.GeneratePollEndedNotifications(note);
	}
}

[ProtoContract]
[ProtoInclude(100, typeof(DriveFileDeleteJob))]
[ProtoInclude(101, typeof(PollExpiryJob))]
public class BackgroundTaskJob : Job;

[ProtoContract]
public class DriveFileDeleteJob : BackgroundTaskJob
{
	[ProtoMember(1)] public required string DriveFileId;
	[ProtoMember(2)] public required bool   Expire;
}

[ProtoContract]
public class PollExpiryJob : BackgroundTaskJob
{
	[ProtoMember(1)] public required string NoteId;
}