using System.Diagnostics.CodeAnalysis;
using Blurhash;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Federation.Cryptography;
using Iceshrimp.Backend.Core.Helpers;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Backend.Core.Queues;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ImageSharp = SixLabors.ImageSharp.Image;

namespace Iceshrimp.Backend.Core.Services;

public class DriveService(
	DatabaseContext db,
	ObjectStorageService storageSvc,
	[SuppressMessage("ReSharper", "SuggestBaseTypeForParameterInConstructor")]
	IOptionsSnapshot<Config.StorageSection> storageConfig,
	IOptions<Config.InstanceSection> instanceConfig,
	HttpClient httpClient,
	QueueService queueSvc,
	ILogger<DriveService> logger
)
{
	public async Task<DriveFile?> StoreFile(
		string? uri, User user, bool sensitive, string? description = null, string? mimeType = null,
		bool logExisting = true
	)
	{
		if (uri == null) return null;

		if (logExisting)
			logger.LogDebug("Storing file {uri} for user {userId}", uri, user.Id);

		try
		{
			// Do we already have the file?
			var file = await db.DriveFiles.FirstOrDefaultAsync(p => p.Uri == uri);
			if (file != null)
			{
				// If the user matches, return the existing file
				if (file.UserId == user.Id)
				{
					if (logExisting)
					{
						logger.LogDebug("File {uri} is already registered for user, returning existing file {id}",
						                uri, file.Id);
					}

					if (file.Comment != description)
					{
						file.Comment = description;
						db.Update(file);
						await db.SaveChangesAsync();
					}

					return file;
				}

				if (!logExisting)
					logger.LogDebug("Storing file {uri} for user {userId}", uri, user.Id);

				// Otherwise, clone the file
				var req = new DriveFileCreationRequest
				{
					Uri         = uri,
					IsSensitive = sensitive,
					Comment     = description,
					Filename    = new Uri(uri).AbsolutePath.Split('/').LastOrDefault() ?? "",
					MimeType    = null! // Not needed in .Clone
				};

				var clonedFile = file.Clone(user, req);

				logger.LogDebug("File {uri} is already registered for different user, returning clone of existing file {id}, stored as {cloneId}",
				                uri, file.Id, clonedFile.Id);

				await db.AddAsync(clonedFile);
				await db.SaveChangesAsync();
				return clonedFile;
			}

			if (!logExisting)
				logger.LogDebug("Storing file {uri} for user {userId}", uri, user.Id);

			var res = await httpClient.GetAsync(uri);

			var request = new DriveFileCreationRequest
			{
				Uri         = uri,
				Filename    = new Uri(uri).AbsolutePath.Split('/').LastOrDefault() ?? "",
				IsSensitive = sensitive,
				Comment     = description,
				MimeType    = CleanMimeType(mimeType ?? res.Content.Headers.ContentType?.MediaType)
			};

			return await StoreFile(await res.Content.ReadAsStreamAsync(), user, request);
		}
		catch (Exception e)
		{
			logger.LogError("Failed to insert file {uri}: {error}", uri, e.Message);
			return null;
		}
	}

	public async Task<DriveFile> StoreFile(Stream data, User user, DriveFileCreationRequest request)
	{
		await using var buf    = new BufferedStream(data);
		var             digest = await DigestHelpers.Sha256DigestAsync(buf);
		logger.LogDebug("Storing file {digest} for user {userId}", digest, user.Id);
		var file = await db.DriveFiles.FirstOrDefaultAsync(p => p.Sha256 == digest);
		if (file is { IsLink: false })
		{
			if (file.UserId == user.Id)
			{
				logger.LogDebug("File {digest} is already registered for user, returning existing file {id}",
				                digest, file.Id);
				return file;
			}

			var clonedFile = file.Clone(user, request);

			logger.LogDebug("File {digest} is already registered for different user, returning clone of existing file {id}, stored as {cloneId}",
			                digest, file.Id, clonedFile.Id);

			await db.AddAsync(clonedFile);
			await db.SaveChangesAsync();
			return clonedFile;
		}

		buf.Seek(0, SeekOrigin.Begin);

		var shouldStore    = storageConfig.Value.MediaRetention != null || user.Host == null;
		var storedInternal = storageConfig.Value.Provider == Enums.FileStorage.Local;

		if (request.Uri == null && user.Host != null)
			throw GracefulException.UnprocessableEntity("Refusing to store file without uri for remote user");

		string? blurhash  = null;
		Stream? thumbnail = null;
		Stream? webpublic = null;

		DriveFile.FileProperties? properties = null;

		var isImage = request.MimeType.StartsWith("image/") || request.MimeType == "image";
		// skip images larger than 10MB
		var isReasonableSize = buf.Length < 10 * 1024 * 1024;

		if (isImage && isReasonableSize)
		{
			try
			{
				var pre        = DateTime.Now;
				var ident      = await ImageSharp.IdentifyAsync(buf);
				var isAnimated = ident.FrameMetadataCollection.Count != 0;
				properties = new DriveFile.FileProperties { Width = ident.Size.Width, Height = ident.Size.Height };

				// Correct mime type
				if (request.MimeType == "image" && ident.Metadata.DecodedImageFormat?.DefaultMimeType != null)
					request.MimeType = ident.Metadata.DecodedImageFormat.DefaultMimeType;

				buf.Seek(0, SeekOrigin.Begin);

				using var image     = NetVips.Image.NewFromStream(buf);
				using var processed = image.Autorot();
				buf.Seek(0, SeekOrigin.Begin);

				try
				{
					// Calculate blurhash using a x200px image for improved performance
					using var blurhashImage = processed.ThumbnailImage(200, 200, NetVips.Enums.Size.Down);
					var       blurBuf       = blurhashImage.WriteToMemory();
					var       blurArr       = new Pixel[blurhashImage.Width, blurhashImage.Height];

					var idx  = 0;
					var incr = image.Bands - 3;
					for (var i = 0; i < blurhashImage.Height; i++)
					{
						for (var j = 0; j < blurhashImage.Width; j++)
						{
							blurArr[j, i] = new Pixel(blurBuf[idx++] / 255d, blurBuf[idx++] / 255d,
							                          blurBuf[idx++] / 255d);
							idx += incr;
						}
					}

					blurhash = Blurhash.Core.Encode(blurArr, 7, 7, new Progress<int>());
				}
				catch (Exception e)
				{
					logger.LogWarning("Failed to generate blurhash for image with mime type {type}: {e}",
					                  request.MimeType, e.Message);
				}

				if (shouldStore)
				{
					// Generate thumbnail
					using var thumbnailImage = processed.ThumbnailImage(1000, 1000, NetVips.Enums.Size.Down);

					thumbnail = new MemoryStream();
					thumbnailImage.WebpsaveStream(thumbnail, 75, false);
					thumbnail.Seek(0, SeekOrigin.Begin);

					// Generate webpublic for local users, if image is not animated
					if (user.Host == null && !isAnimated)
					{
						using var webpublicImage = processed.ThumbnailImage(2048, 2048, NetVips.Enums.Size.Down);

						webpublic = new MemoryStream();
						webpublicImage.WebpsaveStream(webpublic, request.MimeType == "image/png" ? 100 : 75, false);
						webpublic.Seek(0, SeekOrigin.Begin);
					}
				}

				logger.LogTrace("Image processing took {ms} ms", (int)(DateTime.Now - pre).TotalMilliseconds);
			}
			catch (Exception e)
			{
				logger.LogError("Failed to generate thumbnails for image with mime type {type}: {e}",
				                request.MimeType, e.Message);

				// We want to make sure no images are federated out without stripping metadata & converting to webp
				if (user.Host == null) throw;
			}

			buf.Seek(0, SeekOrigin.Begin);
		}

		string  url;
		string? thumbnailUrl = null;
		string? webpublicUrl = null;

		var filename          = GenerateFilenameKeepingExtension(request.Filename);
		var thumbnailFilename = thumbnail != null ? GenerateWebpFilename("thumbnail-") : null;
		var webpublicFilename = webpublic != null ? GenerateWebpFilename("webpublic-") : null;

		if (shouldStore)
		{
			if (storedInternal)
			{
				var pathBase = storageConfig.Value.Local?.Path ??
				               throw new Exception("Local storage path cannot be null");
				var path = Path.Combine(pathBase, filename);

				await using var writer = File.OpenWrite(path);
				await buf.CopyToAsync(writer);
				url = $"https://{instanceConfig.Value.WebDomain}/files/{filename}";

				if (thumbnailFilename != null && thumbnail is { Length: > 0 })
				{
					var             thumbPath   = Path.Combine(pathBase, thumbnailFilename);
					await using var thumbWriter = File.OpenWrite(thumbPath);
					await thumbnail.CopyToAsync(thumbWriter);
					await thumbnail.DisposeAsync();
					thumbnailUrl = $"https://{instanceConfig.Value.WebDomain}/files/{thumbnailFilename}";
				}

				if (webpublicFilename != null && webpublic is { Length: > 0 })
				{
					var             webpPath   = Path.Combine(pathBase, webpublicFilename);
					await using var webpWriter = File.OpenWrite(webpPath);
					await webpublic.CopyToAsync(webpWriter);
					await webpublic.DisposeAsync();
					webpublicUrl = $"https://{instanceConfig.Value.WebDomain}/files/{webpublicFilename}";
				}
			}
			else
			{
				await storageSvc.UploadFileAsync(filename, buf);
				url = storageSvc.GetFilePublicUrl(filename).AbsoluteUri;

				if (thumbnailFilename != null && thumbnail is { Length: > 0 })
				{
					await storageSvc.UploadFileAsync(thumbnailFilename, thumbnail);
					thumbnailUrl = storageSvc.GetFilePublicUrl(thumbnailFilename).AbsoluteUri;
					await thumbnail.DisposeAsync();
				}

				if (webpublicFilename != null && webpublic is { Length: > 0 })
				{
					await storageSvc.UploadFileAsync(webpublicFilename, webpublic);
					webpublicUrl = storageSvc.GetFilePublicUrl(webpublicFilename).AbsoluteUri;
					await webpublic.DisposeAsync();
				}
			}
		}
		else
		{
			url = request.Uri ?? throw new Exception("Uri must not be null at this stage");
		}

		file = new DriveFile
		{
			Id                 = IdHelpers.GenerateSlowflakeId(),
			CreatedAt          = DateTime.UtcNow,
			User               = user,
			UserHost           = user.Host,
			Sha256             = digest,
			Size               = (int)buf.Length,
			IsLink             = !shouldStore && user.Host != null,
			AccessKey          = filename,
			IsSensitive        = request.IsSensitive,
			StoredInternal     = storedInternal,
			Src                = request.Source,
			Uri                = request.Uri,
			Url                = url,
			Name               = request.Filename,
			Comment            = request.Comment,
			Type               = CleanMimeType(request.MimeType),
			RequestHeaders     = request.RequestHeaders,
			RequestIp          = request.RequestIp,
			Blurhash           = blurhash,
			Properties         = properties!,
			ThumbnailUrl       = thumbnailUrl,
			ThumbnailAccessKey = thumbnailFilename,
			WebpublicType      = webpublicUrl != null ? "image/webp" : null,
			WebpublicUrl       = webpublicUrl,
			WebpublicAccessKey = webpublicFilename
		};

		await db.AddAsync(file);
		await db.SaveChangesAsync();

		return file;
	}

	public async Task RemoveFile(DriveFile file)
	{
		await RemoveFile(file.Id);
	}

	public async Task RemoveFile(string fileId)
	{
		var job = new DriveFileDeleteJobData { DriveFileId = fileId, Expire = false };
		await queueSvc.BackgroundTaskQueue.EnqueueAsync(job);
	}

	private static string GenerateFilenameKeepingExtension(string filename)
	{
		var guid = Guid.NewGuid().ToStringLower();
		var ext  = Path.GetExtension(filename);
		return guid + ext;
	}

	private static string GenerateWebpFilename(string prefix = "")
	{
		var guid = Guid.NewGuid().ToStringLower();
		return $"{prefix}{guid}.webp";
	}

	private static string CleanMimeType(string? mimeType)
	{
		return mimeType == null || !Constants.BrowserSafeMimeTypes.Contains(mimeType)
			? "application/octet-stream"
			: mimeType;
	}
}

public class DriveFileCreationRequest
{
	public          string?                     Comment;
	public required string                      Filename = Guid.NewGuid().ToStringLower();
	public required bool                        IsSensitive;
	public required string                      MimeType;
	public          Dictionary<string, string>? RequestHeaders;
	public          string?                     RequestIp;
	public          string?                     Source;
	public          string?                     Uri;
}

//TODO: set uri as well (which may be different)
file static class DriveFileExtensions
{
	public static DriveFile Clone(this DriveFile file, User user, DriveFileCreationRequest request)
	{
		if (file.IsLink)
			throw new Exception("Refusing to clone remote file");

		return new DriveFile
		{
			Id                 = IdHelpers.GenerateSlowflakeId(),
			CreatedAt          = DateTime.UtcNow,
			User               = user,
			Blurhash           = file.Blurhash,
			Type               = file.Type,
			Sha256             = file.Sha256,
			Name               = request.Filename,
			Properties         = file.Properties,
			Size               = file.Size,
			Src                = request.Source,
			IsLink             = false,
			Uri                = request.Uri,
			Url                = file.Url,
			AccessKey          = file.AccessKey,
			ThumbnailUrl       = file.ThumbnailAccessKey,
			IsSensitive        = request.IsSensitive,
			WebpublicType      = file.WebpublicType,
			WebpublicUrl       = file.WebpublicUrl,
			WebpublicAccessKey = file.WebpublicAccessKey,
			StoredInternal     = file.StoredInternal,
			UserHost           = user.Host,
			Comment            = request.Comment,
			RequestHeaders     = request.RequestHeaders,
			RequestIp          = request.RequestIp,
			ThumbnailAccessKey = file.ThumbnailAccessKey
		};
	}
}