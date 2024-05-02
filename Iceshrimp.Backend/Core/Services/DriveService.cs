using System.Diagnostics.CodeAnalysis;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Helpers;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Backend.Core.Queues;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Core.Services;

public class DriveService(
	DatabaseContext db,
	ObjectStorageService storageSvc,
	[SuppressMessage("ReSharper", "SuggestBaseTypeForParameterInConstructor")]
	IOptionsSnapshot<Config.StorageSection> storageConfig,
	IOptions<Config.InstanceSection> instanceConfig,
	HttpClient httpClient,
	QueueService queueSvc,
	ILogger<DriveService> logger,
	ImageProcessor imageProcessor
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

	public async Task<DriveFile> StoreFile(Stream input, User user, DriveFileCreationRequest request)
	{
		if (user.Host == null && input.Length > storageConfig.Value.MaxUploadSizeBytes)
			throw GracefulException.UnprocessableEntity("Attachment is too large.");

		await using var data   = new BufferedStream(input);
		var             digest = await DigestHelpers.Sha256DigestAsync(data);
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

		data.Seek(0, SeekOrigin.Begin);

		var storedInternal = storageConfig.Value.Provider == Enums.FileStorage.Local;

		var shouldCache =
			storageConfig.Value is { MediaRetentionTimeSpan: not null, MediaProcessing.LocalOnly: false } &&
			data.Length <= storageConfig.Value.MaxCacheSizeBytes;

		var shouldStore = user.Host == null || shouldCache;

		if (request.Uri == null && user.Host != null)
			throw GracefulException.UnprocessableEntity("Refusing to store file without uri for remote user");

		string? blurhash = null;

		var properties = new DriveFile.FileProperties();

		string  url;
		string? thumbnailUrl = null;
		string? webpublicUrl = null;

		var isReasonableSize = data.Length < storageConfig.Value.MediaProcessing.MaxFileSizeBytes;
		var isImage          = request.MimeType.StartsWith("image/") || request.MimeType == "image";
		var filename         = GenerateFilenameKeepingExtension(request.Filename);

		string? thumbnailFilename = null;
		string? webpublicFilename = null;

		if (shouldStore)
		{
			if (isImage && isReasonableSize)
			{
				var genWebp = user.Host == null;
				var res     = await imageProcessor.ProcessImage(data, request, true, genWebp);
				properties = res?.Properties ?? properties;

				blurhash          = res?.Blurhash;
				thumbnailFilename = res?.RenderThumbnail != null ? GenerateWebpFilename("thumbnail-") : null;
				webpublicFilename = res?.RenderThumbnail != null ? GenerateWebpFilename("webpublic-") : null;

				if (storedInternal)
				{
					var pathBase = storageConfig.Value.Local?.Path ??
					               throw new Exception("Local storage path cannot be null");
					var path = Path.Combine(pathBase, filename);

					data.Seek(0, SeekOrigin.Begin);
					await using var writer = File.OpenWrite(path);
					await data.CopyToAsync(writer);
					url = $"https://{instanceConfig.Value.WebDomain}/files/{filename}";

					if (thumbnailFilename != null && res?.RenderThumbnail != null)
					{
						var             thumbPath   = Path.Combine(pathBase, thumbnailFilename);
						await using var thumbWriter = File.OpenWrite(thumbPath);
						try
						{
							await res.RenderThumbnail(thumbWriter);
							thumbnailUrl = $"https://{instanceConfig.Value.WebDomain}/files/{thumbnailFilename}";
						}
						catch (Exception e)
						{
							logger.LogDebug("Failed to generate/write thumbnail: {e}", e.Message);
						}
					}

					if (webpublicFilename != null && res?.RenderWebpublic != null)
					{
						var             webpPath   = Path.Combine(pathBase, webpublicFilename);
						await using var webpWriter = File.OpenWrite(webpPath);
						try
						{
							await res.RenderWebpublic(webpWriter);
							webpublicUrl = $"https://{instanceConfig.Value.WebDomain}/files/{webpublicFilename}";
						}
						catch (Exception e)
						{
							logger.LogDebug("Failed to generate/write webp: {e}", e.Message);
						}
					}
				}
				else
				{
					data.Seek(0, SeekOrigin.Begin);
					await storageSvc.UploadFileAsync(filename, data);
					url = storageSvc.GetFilePublicUrl(filename).AbsoluteUri;

					if (thumbnailFilename != null && res?.RenderThumbnail != null)
					{
						try
						{
							await using var stream = new MemoryStream();
							await res.RenderThumbnail(stream);
							stream.Seek(0, SeekOrigin.Begin);
							await storageSvc.UploadFileAsync(thumbnailFilename, stream);
							thumbnailUrl = storageSvc.GetFilePublicUrl(thumbnailFilename).AbsoluteUri;
						}
						catch (Exception e)
						{
							logger.LogDebug("Failed to generate/write thumbnail: {e}", e.Message);
						}
					}

					if (webpublicFilename != null && res?.RenderWebpublic != null)
					{
						try
						{
							await using var stream = new MemoryStream();
							await res.RenderWebpublic(stream);
							stream.Seek(0, SeekOrigin.Begin);
							await storageSvc.UploadFileAsync(webpublicFilename, stream);
							webpublicUrl = storageSvc.GetFilePublicUrl(webpublicFilename).AbsoluteUri;
						}
						catch (Exception e)
						{
							logger.LogDebug("Failed to generate/write thumbnail: {e}", e.Message);
						}
					}
				}
			}
			else
			{
				if (storedInternal)
				{
					var pathBase = storageConfig.Value.Local?.Path ??
					               throw new Exception("Local storage path cannot be null");
					var path = Path.Combine(pathBase, filename);

					await using var writer = File.OpenWrite(path);
					await data.CopyToAsync(writer);
					url = $"https://{instanceConfig.Value.WebDomain}/files/{filename}";
				}
				else
				{
					data.Seek(0, SeekOrigin.Begin);
					await storageSvc.UploadFileAsync(filename, data);
					url = storageSvc.GetFilePublicUrl(filename).AbsoluteUri;
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
			Size               = (int)data.Length,
			IsLink             = !shouldStore,
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
			Properties         = properties,
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