using System.Diagnostics.CodeAnalysis;
using Blurhash.ImageSharp;
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
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

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
		var buf    = new BufferedStream(data);
		var digest = await DigestHelpers.Sha256DigestAsync(buf);
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

		if (request.MimeType.StartsWith("image/") || request.MimeType == "image")
		{
			try
			{
				var image = await Image.LoadAsync<Rgba32>(buf);
				image.Mutate(x => x.AutoOrient());

				// Calculate blurhash using a x200px image for improved performance
				var blurhashImage = image.Clone();
				blurhashImage.Mutate(p => p.Resize(image.Width > image.Height ? new Size(200, 0) : new Size(0, 200)));
				blurhash = Blurhasher.Encode(blurhashImage, 7, 7);

				// Correct mime type
				if (request.MimeType == "image" && image.Metadata.DecodedImageFormat?.DefaultMimeType != null)
					request.MimeType = image.Metadata.DecodedImageFormat.DefaultMimeType;

				properties = new DriveFile.FileProperties { Width = image.Size.Width, Height = image.Size.Height };

				if (shouldStore)
				{
					// Generate thumbnail
					var thumbnailImage = image.Clone();
					thumbnailImage.Metadata.ExifProfile = null;
					thumbnailImage.Metadata.XmpProfile  = null;
					if (Math.Max(image.Size.Width, image.Size.Height) > 1000)
						thumbnailImage.Mutate(p => p.Resize(image.Width > image.Height
							                                    ? new Size(1000, 0)
							                                    : new Size(0, 1000)));

					thumbnail = new MemoryStream();
					var thumbEncoder = new WebpEncoder { Quality = 75, FileFormat = WebpFileFormatType.Lossy };
					await thumbnailImage.SaveAsWebpAsync(thumbnail, thumbEncoder);
					thumbnail.Seek(0, SeekOrigin.Begin);

					// Generate webpublic for local users
					if (user.Host == null)
					{
						var webpublicImage = image.Clone();
						webpublicImage.Metadata.ExifProfile = null;
						webpublicImage.Metadata.XmpProfile  = null;
						if (Math.Max(image.Size.Width, image.Size.Height) > 2048)
							webpublicImage.Mutate(p => p.Resize(image.Width > image.Height
								                                    ? new Size(2048, 0)
								                                    : new Size(0, 2048)));

						var encoder = new WebpEncoder
						{
							Quality    = request.MimeType == "image/png" ? 100 : 75,
							FileFormat = WebpFileFormatType.Lossy
						};

						webpublic = new MemoryStream();
						await webpublicImage.SaveAsWebpAsync(webpublic, encoder);
						webpublic.Seek(0, SeekOrigin.Begin);
					}
				}
			}
			catch
			{
				logger.LogError("Failed to generate blurhash & thumbnail for image with mime type {type}",
				                request.MimeType);

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
				}

				if (webpublicFilename != null && webpublic is { Length: > 0 })
				{
					var             webpPath   = Path.Combine(pathBase, webpublicFilename);
					await using var webpWriter = File.OpenWrite(webpPath);
					await webpublic.CopyToAsync(webpWriter);
				}
			}
			else
			{
				await storageSvc.UploadFileAsync(filename, data);
				url = storageSvc.GetFilePublicUrl(filename).AbsoluteUri;

				if (thumbnailFilename != null && thumbnail is { Length: > 0 })
				{
					await storageSvc.UploadFileAsync(thumbnailFilename, thumbnail);
					thumbnailUrl = storageSvc.GetFilePublicUrl(thumbnailFilename).AbsoluteUri;
				}

				if (webpublicFilename != null && webpublic is { Length: > 0 })
				{
					await storageSvc.UploadFileAsync(webpublicFilename, webpublic);
					webpublicUrl = storageSvc.GetFilePublicUrl(webpublicFilename).AbsoluteUri;
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