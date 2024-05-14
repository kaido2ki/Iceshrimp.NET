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
		var total     = await db.DriveFiles.CountAsync(p => p.StoredInternal && !p.IsLink);
		var completed = 0;
		logger.LogInformation("Found {total} files. Migrating in batches...", total);
		while (true)
		{
			var keys = await db.DriveFiles
			                   .Where(p => p.StoredInternal && !p.IsLink)
			                   .GroupBy(p => p.AccessKey)
			                   .Select(p => p.Key)
			                   .Take(100)
			                   .ToListAsync();
			
			var hits = await db.DriveFiles
			                   .Where(p => p.StoredInternal && !p.IsLink && keys.Contains(p.AccessKey))
			                   .GroupBy(p => p.AccessKey)
			                   .ToListAsync();

			if (hits.Count == 0) break;

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

		[SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
		async ValueTask MigrateFile(IEnumerable<DriveFile> files, CancellationToken token)
		{
			var file = files.FirstOrDefault();
			if (file == null) return;

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

			foreach (var item in files)
			{
				item.StoredInternal = false;
				item.Url            = file.Url;
				item.ThumbnailUrl   = file.ThumbnailUrl;
				item.WebpublicUrl   = file.WebpublicUrl;
			}
		}
	}
}