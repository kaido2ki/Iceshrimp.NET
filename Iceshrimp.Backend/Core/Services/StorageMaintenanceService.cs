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
	public async Task MigrateLocalFiles()
	{
		var pathBase      = options.Value.Local?.Path;
		var pathsToDelete = new ConcurrentBag<string>();

		logger.LogInformation("Migrating all files to object storage...");
		var total     = await db.DriveFiles.CountAsync(p => p.StoredInternal);
		var completed = 0;
		logger.LogInformation("Found {total} files. Migrating in batches...", total);
		while (total - completed > 0)
		{
			var hits = await db.DriveFiles.Where(p => p.StoredInternal && !p.IsLink)
			                   .OrderBy(p => p.Id)
			                   .Skip(completed)
			                   .Take(100)
			                   .ToListAsync();

			await Parallel.ForEachAsync(hits, new ParallelOptions { MaxDegreeOfParallelism = 8 }, MigrateFile);
			await db.SaveChangesAsync();
			foreach (var path in pathsToDelete)
				File.Delete(path);

			completed += hits.Count;
			pathsToDelete.Clear();
			db.ChangeTracker.Clear();
			logger.LogInformation("Migrating files to object storage... {completed}/{total}", completed, total);
		}

		logger.LogInformation("Done! All files have been migrated.");

		return;

		async ValueTask MigrateFile(DriveFile file, CancellationToken token)
		{
			if (file.AccessKey != null)
			{
				var path   = Path.Join(pathBase, file.AccessKey);
				var stream = File.OpenRead(path);

				await objectStorageSvc.UploadFileAsync(file.AccessKey, file.Type, stream);
				file.Url = objectStorageSvc.GetFilePublicUrl(file.AccessKey).AbsoluteUri;
				pathsToDelete.Add(path);
			}

			if (file.ThumbnailAccessKey != null)
			{
				var path   = Path.Join(pathBase, file.ThumbnailAccessKey);
				var stream = File.OpenRead(path);

				await objectStorageSvc.UploadFileAsync(file.ThumbnailAccessKey, "image/webp", stream);
				file.ThumbnailUrl = objectStorageSvc.GetFilePublicUrl(file.ThumbnailAccessKey).AbsoluteUri;
				pathsToDelete.Add(path);
			}

			if (file.WebpublicAccessKey != null)
			{
				var path   = Path.Join(pathBase, file.WebpublicAccessKey);
				var stream = File.OpenRead(path);

				await objectStorageSvc.UploadFileAsync(file.WebpublicAccessKey, "image/webp", stream);
				file.WebpublicUrl = objectStorageSvc.GetFilePublicUrl(file.WebpublicAccessKey).AbsoluteUri;
				pathsToDelete.Add(path);
			}

			file.StoredInternal = false;
		}
	}
}