using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;
using JR = System.Text.Json.Serialization.JsonRequiredAttribute;

namespace Iceshrimp.Backend.Core.Queues;

public class BackgroundTaskQueue()
	: PostgresJobQueue<BackgroundTaskJobData>("background-task", BackgroundTaskQueueProcessorDelegateAsync, 4)
{
	private static async Task BackgroundTaskQueueProcessorDelegateAsync(
		Job job,
		BackgroundTaskJobData jobData,
		IServiceProvider scope,
		CancellationToken token
	)
	{
		switch (jobData)
		{
			case DriveFileDeleteJobData { Expire: true } driveFileDeleteJob:
				await ProcessDriveFileExpire(driveFileDeleteJob, scope, token);
				break;
			case DriveFileDeleteJobData driveFileDeleteJob:
				await ProcessDriveFileDelete(driveFileDeleteJob, scope, token);
				break;
			case PollExpiryJobData pollExpiryJob:
				await ProcessPollExpiry(pollExpiryJob, scope, token);
				break;
			case MuteExpiryJobData muteExpiryJob:
				await ProcessMuteExpiry(muteExpiryJob, scope, token);
				break;
		}
	}

	private static async Task ProcessDriveFileDelete(
		DriveFileDeleteJobData jobData,
		IServiceProvider scope,
		CancellationToken token
	)
	{
		var db = scope.GetRequiredService<DatabaseContext>();
		var usedAsAvatarOrBanner =
			await db.Users.AnyAsync(p => p.AvatarId == jobData.DriveFileId ||
			                             p.BannerId == jobData.DriveFileId, token);

		var usedInNote = await db.Notes.AnyAsync(p => p.FileIds.Contains(jobData.DriveFileId), token);

		if (!usedAsAvatarOrBanner && !usedInNote)
		{
			var file = await db.DriveFiles.FirstOrDefaultAsync(p => p.Id == jobData.DriveFileId, token);
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
		DriveFileDeleteJobData jobData,
		IServiceProvider scope,
		CancellationToken token
	)
	{
		var db     = scope.GetRequiredService<DatabaseContext>();
		var logger = scope.GetRequiredService<ILogger<BackgroundTaskQueue>>();
		logger.LogDebug("Expiring file {id}...", jobData.DriveFileId);

		var file = await db.DriveFiles.FirstOrDefaultAsync(p => p.Id == jobData.DriveFileId, token);
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

	[SuppressMessage("ReSharper", "EntityFramework.NPlusOne.IncompleteDataQuery",
	                 Justification = "IncludeCommonProperties()")]
	[SuppressMessage("ReSharper", "EntityFramework.NPlusOne.IncompleteDataUsage", Justification = "Same as above")]
	private static async Task ProcessPollExpiry(
		PollExpiryJobData jobData,
		IServiceProvider scope,
		CancellationToken token
	)
	{
		var db   = scope.GetRequiredService<DatabaseContext>();
		var poll = await db.Polls.FirstOrDefaultAsync(p => p.NoteId == jobData.NoteId, token);
		if (poll == null) return;
		if (poll.ExpiresAt > DateTime.UtcNow + TimeSpan.FromSeconds(30)) return;
		var note = await db.Notes.IncludeCommonProperties()
		                   .FirstOrDefaultAsync(p => p.Id == poll.NoteId, token);
		if (note == null) return;

		var notificationSvc = scope.GetRequiredService<NotificationService>();
		await notificationSvc.GeneratePollEndedNotifications(note);
		if (note.User.Host == null)
		{
			var voters = await db.PollVotes.Where(p => p.Note == note && p.User.Host != null)
			                     .Select(p => p.User)
			                     .ToListAsync(token);

			if (voters.Count == 0) return;

			var userRenderer = scope.GetRequiredService<ActivityPub.UserRenderer>();
			var noteRenderer = scope.GetRequiredService<ActivityPub.NoteRenderer>();
			var deliverSvc   = scope.GetRequiredService<ActivityPub.ActivityDeliverService>();

			var actor    = userRenderer.RenderLite(note.User);
			var rendered = await noteRenderer.RenderAsync(note);
			var activity = ActivityPub.ActivityRenderer.RenderUpdate(rendered, actor);

			await deliverSvc.DeliverToAsync(activity, note.User, voters.ToArray());
		}
	}

	private static async Task ProcessMuteExpiry(
		MuteExpiryJobData jobData,
		IServiceProvider scope,
		CancellationToken token
	)
	{
		var db = scope.GetRequiredService<DatabaseContext>();
		var muting = await db.Mutings.Include(muting => muting.Mutee)
		                     .Include(muting => muting.Muter)
		                     .FirstOrDefaultAsync(p => p.Id == jobData.MuteId, token);

		if (muting is not { ExpiresAt: not null }) return;
		if (muting.ExpiresAt > DateTime.UtcNow + TimeSpan.FromSeconds(30)) return;

		db.Remove(muting);
		await db.SaveChangesAsync(token);
		var eventSvc = scope.GetRequiredService<EventService>();
		eventSvc.RaiseUserUnmuted(null, muting.Muter, muting.Mutee);
	}
}

[JsonDerivedType(typeof(DriveFileDeleteJobData), "driveFileDelete")]
[JsonDerivedType(typeof(PollExpiryJobData), "pollExpiry")]
[JsonDerivedType(typeof(MuteExpiryJobData), "muteExpiry")]
public abstract class BackgroundTaskJobData;

public class DriveFileDeleteJobData : BackgroundTaskJobData
{
	[JR] [J("driveFileId")] public required string DriveFileId { get; set; }
	[JR] [J("expire")]      public required bool   Expire      { get; set; }
}

public class PollExpiryJobData : BackgroundTaskJobData
{
	[JR] [J("noteId")] public required string NoteId { get; set; }
}

public class MuteExpiryJobData : BackgroundTaskJobData
{
	[JR] [J("muteId")] public required string MuteId { get; set; }
}