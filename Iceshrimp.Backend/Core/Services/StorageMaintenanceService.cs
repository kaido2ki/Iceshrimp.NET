using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Core.Services;

public class StorageMaintenanceService(
	DatabaseContext db,
	ObjectStorageService objectStorageSvc,
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
				var path           = Path.Join(pathBase, file.ThumbnailAccessKey);
				var stream         = File.OpenRead(path);
				var TheImageFormat = options.Value.MediaProcessing.DefaultImageFormat;
				var filename       = file.Name.EndsWith(".webp") ? file.Name : $"{file.Name}.webp";
				if (TheImageFormat == 1)
					filename = file.Name.EndsWith(".jpeg") ? file.Name : $"{file.Name}.jpeg";
					await objectStorageSvc.UploadFileAsync(file.ThumbnailAccessKey, "image/jpeg", filename, stream);
				if (TheImageFormat == 2)
					filename = file.Name.EndsWith(".webp") ? file.Name : $"{file.Name}.webp";
					await objectStorageSvc.UploadFileAsync(file.ThumbnailAccessKey, "image/webp", filename, stream);
				if (TheImageFormat == 3)
					filename = file.Name.EndsWith(".avif") ? file.Name : $"{file.Name}.avif";
					await objectStorageSvc.UploadFileAsync(file.ThumbnailAccessKey, "image/avif", filename, stream);
				if (TheImageFormat == 4)
					filename = file.Name.EndsWith(".jxl") ? file.Name : $"{file.Name}.jxl";
					await objectStorageSvc.UploadFileAsync(file.ThumbnailAccessKey, "image/jxl", filename, stream);
				if (TheImageFormat != 1 && TheImageFormat != 2 && TheImageFormat !=3 && TheImageFormat !=4)
					await objectStorageSvc.UploadFileAsync(file.ThumbnailAccessKey, "image/webp", filename, stream);
				
				file.ThumbnailUrl = objectStorageSvc.GetFilePublicUrl(file.ThumbnailAccessKey).AbsoluteUri;
				deletionQueue.Add(path);
			}

			if (file.WebpublicAccessKey != null)
			{
				var path           = Path.Join(pathBase, file.WebpublicAccessKey);
				var stream         = File.OpenRead(path);
				var filename       = file.Name.EndsWith(".webp") ? file.Name : $"{file.Name}.webp";
				var TheImageFormat = options.Value.MediaProcessing.DefaultImageFormat;
				
				if (TheImageFormat == 1)
					filename = file.Name.EndsWith(".jpeg") ? file.Name : $"{file.Name}.jpeg";
				await objectStorageSvc.UploadFileAsync(file.WebpublicAccessKey, "image/jpeg", filename, stream);
				if (TheImageFormat == 2)
					filename = file.Name.EndsWith(".webp") ? file.Name : $"{file.Name}.webp";
				await objectStorageSvc.UploadFileAsync(file.WebpublicAccessKey, "image/webp", filename, stream);
				if (TheImageFormat == 3)
					filename = file.Name.EndsWith(".avif") ? file.Name : $"{file.Name}.avif";
				await objectStorageSvc.UploadFileAsync(file.WebpublicAccessKey, "image/avif", filename, stream);
				if (TheImageFormat == 4)
					filename = file.Name.EndsWith(".jxl") ? file.Name : $"{file.Name}.jxl";
				await objectStorageSvc.UploadFileAsync(file.WebpublicAccessKey, "image/jxl", filename, stream);
				if (TheImageFormat != 1 && TheImageFormat != 2 && TheImageFormat !=3 && TheImageFormat !=4)
					await objectStorageSvc.UploadFileAsync(file.WebpublicAccessKey, "image/webp", filename, stream);
				file.WebpublicUrl = objectStorageSvc.GetFilePublicUrl(file.WebpublicAccessKey).AbsoluteUri;
				deletionQueue.Add(path);
			}

			foreach (var item in files)
			{
				item.StoredInternal = false;
				item.Url            = file.Url;
				item.ThumbnailUrl   = file.ThumbnailUrl;
				item.WebpublicUrl   = file.WebpublicUrl;
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
}