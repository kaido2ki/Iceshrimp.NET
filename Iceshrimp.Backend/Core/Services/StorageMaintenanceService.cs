using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Core.Services;

public class StorageMaintenanceService(
	DatabaseContext db,
	ObjectStorageService objectStorageSvc,
	DriveService driveSvc,
	[SuppressMessage("ReSharper", "SuggestBaseTypeForParameterInConstructor")]
	IOptionsSnapshot<Config.StorageSection> options,
	ILogger<StorageMaintenanceService> logger
)
{
	public async Task MigrateLocalFiles(bool purge)
	{
		var pathBase      = options.Value.Local?.Path;
		var pathsToDelete = new ConcurrentBag<string>();
		var failed        = new ConcurrentBag<string>();

		logger.LogInformation("Migrating all files to object storage...");
		var total     = await db.DriveFiles.CountAsync(p => p.StoredInternal && !p.IsLink);
		var completed = 0;
		logger.LogInformation("Found {total} files. Migrating in batches...", total);
		while (true)
		{
			var keys = await db.DriveFiles
			                   .Where(p => p.StoredInternal && !p.IsLink)
			                   .Where(p => !failed.Contains(p.Id))
			                   .GroupBy(p => p.AccessKey)
			                   .Select(p => p.Key)
			                   .OrderBy(p => p)
			                   .Take(100)
			                   .ToListAsync();

			var hits = await db.DriveFiles
			                   .Where(p => p.StoredInternal && !p.IsLink && keys.Contains(p.AccessKey))
			                   .GroupBy(p => p.AccessKey)
			                   .ToListAsync();

			if (hits.Count == 0) break;

			await Parallel.ForEachAsync(hits, new ParallelOptions { MaxDegreeOfParallelism = 8 }, TryMigrateFile);
			await db.SaveChangesAsync();
			foreach (var path in pathsToDelete)
				File.Delete(path);

			completed += hits.Count;
			pathsToDelete.Clear();
			db.ChangeTracker.Clear();
			logger.LogInformation("Migrating files to object storage... {completed}/{total}", completed, total);
		}

		if (failed.IsEmpty)
			logger.LogInformation("Done! All files have been migrated.");
		else if (!purge)
			logger.LogInformation("Done. Some files could not be migrated successfully. You may retry this process or clean them up by adding --purge to the CLI arguments.");
		else
			await PurgeFiles();

		return;

		[SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
		async ValueTask TryMigrateFile(IEnumerable<DriveFile> files, CancellationToken token)
		{
			try
			{
				await MigrateFile(files).WaitAsync(token);
			}
			catch (Exception e)
			{
				foreach (var file in files)
				{
					logger.LogWarning("Failed to migrate file {id}: {error}", file.Id, e.Message);
					failed.Add(file.Id);
				}
			}
		}

		[SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
		async Task MigrateFile(IEnumerable<DriveFile> files)
		{
			var file = files.FirstOrDefault();
			if (file == null) return;

			// defer deletions in case an error occurs
			List<string> deletionQueue = [];

			if (file.AccessKey != null)
			{
				var path   = Path.Join(pathBase, file.AccessKey);
				var stream = File.OpenRead(path);

				await objectStorageSvc.UploadFileAsync(file.AccessKey, file.Type, file.Name, stream);
				file.Url = objectStorageSvc.GetFilePublicUrl(file.AccessKey).AbsoluteUri;
				deletionQueue.Add(path);
			}

			if (file.ThumbnailAccessKey != null)
			{
				var path     = Path.Join(pathBase, file.ThumbnailAccessKey);
				var stream   = File.OpenRead(path);
				var filename = file.Name.EndsWith(".webp") ? file.Name : $"{file.Name}.webp";

				await objectStorageSvc.UploadFileAsync(file.ThumbnailAccessKey, "image/webp", filename, stream);
				file.ThumbnailUrl = objectStorageSvc.GetFilePublicUrl(file.ThumbnailAccessKey).AbsoluteUri;
				deletionQueue.Add(path);
			}

			if (file.PublicAccessKey != null)
			{
				var path     = Path.Join(pathBase, file.PublicAccessKey);
				var stream   = File.OpenRead(path);
				var filename = file.Name.EndsWith(".webp") ? file.Name : $"{file.Name}.webp";

				await objectStorageSvc.UploadFileAsync(file.PublicAccessKey, "image/webp", filename, stream);
				file.PublicUrl = objectStorageSvc.GetFilePublicUrl(file.PublicAccessKey).AbsoluteUri;
				deletionQueue.Add(path);
			}

			foreach (var item in files)
			{
				item.StoredInternal = false;
				item.Url            = file.Url;
				item.ThumbnailUrl   = file.ThumbnailUrl;
				item.PublicUrl      = file.PublicUrl;
			}

			foreach (var item in deletionQueue) pathsToDelete.Add(item);
		}

		async Task PurgeFiles()
		{
			logger.LogInformation("Done. Purging {count} failed files...", failed.Count);
			foreach (var chunk in failed.Chunk(100))
				await db.DriveFiles.Where(p => chunk.Contains(p.Id)).ExecuteDeleteAsync();
			logger.LogInformation("All done.");
		}
	}

	public async Task FixupMedia(bool dryRun)
	{
		var query    = db.DriveFiles.Where(p => !p.IsLink && p.Uri != null);
		var total    = await query.CountAsync();
		var progress = 0;
		var modified = 0;
		logger.LogInformation("Validating all files, this may take a long time...");

		await foreach (var file in query.AsChunkedAsyncEnumerable(50, p => p.Id))
		{
			if (++progress % 100 == 0)
				logger.LogInformation("Validating files... ({idx}/{total})", progress, total);

			var res = await driveSvc.VerifyFileExistence(file);
			if (res == (true, true, true)) continue;

			modified++;

			if (!res.original)
			{
				if (dryRun)
				{
					logger.LogInformation("Would expire file {id}, but --dry-run was specified.", file.Id);
					continue;
				}

				await driveSvc.ExpireFile(file);
				continue;
			}

			if (!res.thumbnail)
			{
				if (dryRun)
				{
					logger.LogInformation("Would remove thumbnail for {id}, but --dry-run was specified.", file.Id);
				}
				else
				{
					file.ThumbnailAccessKey = null;
					file.ThumbnailUrl       = null;
					file.ThumbnailMimeType  = null;
					await db.SaveChangesAsync();
				}
			}

			if (!res.@public)
			{
				if (dryRun)
				{
					logger.LogInformation("Would remove public version for {id}, but --dry-run was specified.",
					                      file.Id);
				}
				else
				{
					file.PublicAccessKey = null;
					file.PublicUrl       = null;
					file.PublicMimeType  = null;
					await db.SaveChangesAsync();
				}
			}
		}

		if (dryRun)
		{
			logger.LogInformation("Finished validating {count} files, of which {count} would have been fixed up.",
			                      progress, modified);
		}
		else
		{
			logger.LogInformation("Finished validating {count} files, of which {count} have been fixed up.",
			                      progress, modified);
		}
	}
}