using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Helpers;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Backend.Core.Queues;
using Iceshrimp.Backend.Core.Services.ImageProcessing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using static Iceshrimp.Backend.Core.Services.ImageProcessing.ImageVersion;

namespace Iceshrimp.Backend.Core.Services;

using ImageVerTriple = (ImageVersion format, string accessKey, string url);

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
) : IScopedService
{
	public async Task<DriveFile?> StoreFileAsync(
		string? uri, User user, bool sensitive, string? description = null, string? mimeType = null,
		bool logExisting = true, bool forceStore = false, bool skipImageProcessing = false
	)
	{
		if (uri == null) return null;

		if (logExisting)
			logger.LogDebug("Storing file {uri} for user {userId}", uri, user.Id);

		if (string.IsNullOrWhiteSpace(description))
			description = null;

		try
		{
			// Do we already have the file?
			DriveFile? file = null;
			if (!forceStore)
				file = await db.DriveFiles.FirstOrDefaultAsync(p => p.Uri == uri && (!p.IsLink || p.UserId == user.Id));

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
					Filename    = file.Name,
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

			try
			{
				var res = await httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead);
				res.EnsureSuccessStatusCode();

				var filename = res.Content.Headers.ContentDisposition?.FileName
				               ?? new Uri(uri).AbsolutePath.Split('/').LastOrDefault() ?? "";

				var request = new DriveFileCreationRequest
				{
					Uri         = uri,
					Filename    = filename,
					IsSensitive = sensitive,
					Comment     = description,
					MimeType    = CleanMimeType(res.Content.Headers.ContentType?.MediaType ?? mimeType)
				};

				var input = await res.Content.ReadAsStreamAsync();
				var maxLength = user.IsLocalUser
					? storageConfig.Value.MaxUploadSizeBytes
					: storageConfig.Value.MediaRetentionTimeSpan != null
						? storageConfig.Value.MaxCacheSizeBytes
						: 0;

				var stream = await GetSafeStreamOrNullAsync(input, maxLength, res.Content.Headers.ContentLength);
				try
				{
					return await StoreFileAsync(stream, user, request, skipImageProcessing);
				}
				catch (Exception e)
				{
					logger.LogWarning("Failed to store downloaded file from {uri}: {error}, storing as link", uri, e);
					throw;
				}
			}
			catch (Exception e)
			{
				logger.LogDebug("Failed to download file from {uri}: {error}, storing as link", uri, e.Message);
				file = new DriveFile
				{
					Id             = IdHelpers.GenerateSnowflakeId(),
					CreatedAt      = DateTime.UtcNow,
					User           = user,
					UserHost       = user.Host,
					Size           = 0,
					IsLink         = true,
					IsSensitive    = sensitive,
					StoredInternal = false,
					Uri            = uri,
					Url            = uri,
					Name           = new Uri(uri).AbsolutePath.Split('/').LastOrDefault() ?? "",
					Comment        = description,
					Type           = CleanMimeType(mimeType ?? "application/octet-stream"),
					AccessKey      = Guid.NewGuid().ToStringLower()
				};

				db.Add(file);
				await db.SaveChangesAsync();
				return file;
			}
		}
		catch (Exception e)
		{
			logger.LogError("Failed to insert file {uri}: {error}", uri, e.Message);
			return null;
		}
	}

	public async Task<DriveFile> StoreFileAsync(
		Stream input, User user, DriveFileCreationRequest request, bool skipImageProcessing = false
	)
	{
		if (user.IsLocalUser && input.Length > storageConfig.Value.MaxUploadSizeBytes)
			throw GracefulException.UnprocessableEntity("Attachment is too large.");

		DriveFile? file;
		request.Filename = request.Filename.Trim('"');
		if (input == Stream.Null || (user.IsRemoteUser && input.Length > storageConfig.Value.MaxCacheSizeBytes))
		{
			file = new DriveFile
			{
				Id             = IdHelpers.GenerateSnowflakeId(),
				CreatedAt      = DateTime.UtcNow,
				User           = user,
				UserHost       = user.Host,
				Size           = (int)input.Length,
				IsLink         = true,
				IsSensitive    = request.IsSensitive,
				StoredInternal = false,
				Src            = request.Source,
				Uri            = request.Uri,
				Url            = request.Uri ?? throw new Exception("Cannot store remote attachment without URI"),
				Name           = request.Filename,
				Comment        = request.Comment,
				Type           = CleanMimeType(request.MimeType),
				RequestHeaders = request.RequestHeaders,
				RequestIp      = request.RequestIp,
				AccessKey      = Guid.NewGuid().ToStringLower() + Path.GetExtension(request.Filename)
			};

			db.Add(file);
			await db.SaveChangesAsync();
			return file;
		}

		var buf = new byte[input.Length];
		using (var memoryStream = new MemoryStream(buf))
			await input.CopyToAsync(memoryStream);

		var digest = await DigestHelpers.Sha256DigestAsync(buf);
		logger.LogDebug("Storing file {digest} for user {userId}", digest, user.Id);
		file = await db.DriveFiles.FirstOrDefaultAsync(p => p.Sha256 == digest && (!p.IsLink || p.UserId == user.Id));
		if (file != null)
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

		var storedInternal = storageConfig.Value.Provider == Enums.FileStorage.Local;

		var shouldCache =
			storageConfig.Value is { MediaRetentionTimeSpan: not null, MediaProcessing.LocalOnly: false }
			&& buf.Length <= storageConfig.Value.MaxCacheSizeBytes;

		var shouldStore = user.IsLocalUser || shouldCache;

		if (request.Uri == null && user.IsRemoteUser)
			throw GracefulException.UnprocessableEntity("Refusing to store file without uri for remote user");

		string? blurhash = null;

		var properties = new DriveFile.FileProperties();

		ImageVerTriple? original  = null;
		ImageVerTriple? thumbnail = null;
		ImageVerTriple? @public   = null;

		var isReasonableSize = buf.Length < storageConfig.Value.MediaProcessing.MaxFileSizeBytes;
		var isImage          = request.MimeType.StartsWith("image/") || request.MimeType == "image";

		if (shouldStore)
		{
			if (isImage && isReasonableSize)
			{
				var ident = imageProcessor.IdentifyImage(buf, request);
				if (ident == null)
				{
					logger.LogWarning("imageProcessor.IdentifyImage() returned null, skipping image processing");
					original = await StoreOriginalFileOnly(input, request);
				}
				else
				{
					if (ident.IsAnimated)
					{
						logger.LogDebug("Image is animated, bypassing image processing...");
						skipImageProcessing = true;
					}
					else if (ident.Width * ident.Height > storageConfig.Value.MediaProcessing.MaxResolutionPx)
					{
						var config = storageConfig.Value.MediaProcessing;
						if (config.FailIfImageExceedsMaxRes)
						{
							// @formatter:off
							throw GracefulException.UnprocessableEntity($"Image is larger than {config.MaxResolutionMpx}mpx. Please resize your image to fit within the allowed dimensions.");
							// @formatter:on
						}

						logger.LogDebug("Image is larger than {mpx}mpx ({width}x{height}), bypassing image processing...",
						                config.MaxResolutionMpx, ident.Width, ident.Height);
						skipImageProcessing = true;
					}

					var formats = GetFormats(user, request, skipImageProcessing);

					var res = imageProcessor.ProcessImage(buf, ident, request, formats);
					properties = res;
					blurhash   = res.Blurhash;

					var processed = await res.RequestedFormats
					                         .Select(p => ProcessAndStoreFileVersionAsync(p.Key, p.Value,
						                                 request.Filename))
					                         .AwaitAllNoConcurrencyAsync()
					                         .ContinueWithResult(p => p.ToImmutableArray());

					original = processed.FirstOrDefault(p => p?.format.Key == KeyEnum.Original)
					           ?? throw new Exception("Image processing didn't result in an original version");

					thumbnail = processed.FirstOrDefault(p => p?.format.Key == KeyEnum.Thumbnail);
					@public   = processed.FirstOrDefault(p => p?.format.Key == KeyEnum.Public);

					if (@public == null && user.IsLocalUser && !skipImageProcessing)
					{
						var publicLocalFormat = storageConfig.Value.MediaProcessing.ImagePipeline.Public.Local.Format;
						if (publicLocalFormat is not ImageFormatEnum.Keep and not ImageFormatEnum.None)
							throw new Exception("Failed to re-encode image, bailing due to risk of metadata leakage");
					}
				}
			}
			else
			{
				original = await StoreOriginalFileOnly(input, request);
			}
		}
		else
		{
			if (request.Uri == null)
				throw new Exception("Uri must not be null at this stage");
		}

		if (original?.format.Format is { } fmt and not ImageFormat.Keep)
		{
			request.MimeType =  fmt.MimeType;
			request.Filename += $".{fmt.Extension}";
		}

		file = new DriveFile
		{
			Id                 = IdHelpers.GenerateSnowflakeId(),
			CreatedAt          = DateTime.UtcNow,
			User               = user,
			UserHost           = user.Host,
			Sha256             = digest,
			Size               = buf.Length,
			IsLink             = !shouldStore,
			AccessKey          = original?.accessKey ?? Guid.NewGuid().ToStringLower(),
			IsSensitive        = request.IsSensitive,
			StoredInternal     = storedInternal,
			Src                = request.Source,
			Uri                = request.Uri,
			Url                = original?.url ?? request.Uri ?? throw new Exception("Uri must not be null here"),
			Name               = request.Filename,
			Comment            = request.Comment,
			Type               = CleanMimeType(request.MimeType),
			RequestHeaders     = request.RequestHeaders,
			RequestIp          = request.RequestIp,
			Blurhash           = blurhash,
			Properties         = properties,
			ThumbnailUrl       = thumbnail?.url,
			ThumbnailAccessKey = thumbnail?.accessKey,
			ThumbnailMimeType  = thumbnail?.format.Format.MimeType,
			PublicUrl          = @public?.url,
			PublicAccessKey    = @public?.accessKey,
			PublicMimeType     = @public?.format.Format.MimeType
		};

		await db.AddAsync(file);
		await db.SaveChangesAsync();

		return file;
	}

	private async Task<ImageVerTriple?> StoreOriginalFileOnly(
		Stream input, DriveFileCreationRequest request
	)
	{
		var accessKey = GenerateAccessKey(extension: Path.GetExtension(request.Filename).TrimStart('.')).TrimStart('-');
		var url       = await StoreFileVersionAsync(input, accessKey, request.Filename, request.MimeType);
		return (Stub, accessKey, url);
	}

	private async Task<ImageVerTriple?> ProcessAndStoreFileVersionAsync(
		ImageVersion version, Func<Task<Stream>>? encode, string fileName
	)
	{
		if (encode == null) return null;
		var     accessKey = GenerateAccessKey(version.Key.ToString().ToLowerInvariant(), version.Format.Extension);
		Stream? stream    = null;
		try
		{
			try
			{
				var sw = Stopwatch.StartNew();
				stream = await encode();
				sw.Stop();
				logger.LogDebug("Encoding {version} image took {ms} ms",
				                version.Key.ToString().ToLowerInvariant(), sw.ElapsedMilliseconds);
			}
			catch (Exception e)
			{
				logger.LogWarning("Failed to process {ext} file version: {e}", version.Format.Extension, e.Message);
				return null;
			}

			fileName = GenerateDerivedFileName(fileName, version.Format.Extension);
			var url = await StoreFileVersionAsync(stream, accessKey, fileName, version.Format.MimeType);
			return (version, accessKey, url);
		}
		finally
		{
			if (stream != null)
				await stream.DisposeAsync();
		}
	}

	private Task<string> StoreFileVersionAsync(Stream stream, string accessKey, string fileName, string mimeType)
	{
		// @formatter:off
		return storageConfig.Value.Provider switch
		{
			Enums.FileStorage.Local         => StoreFileVersionLocalStorageAsync(stream, accessKey),
			Enums.FileStorage.ObjectStorage => StoreFileVersionObjectStorageAsync(stream, accessKey, fileName, mimeType),
			_                               => throw new ArgumentOutOfRangeException()
		};
		// @formatter:on
	}

	private async Task<string> StoreFileVersionLocalStorageAsync(Stream stream, string filename)
	{
		var pathBase = storageConfig.Value.Local?.Path ?? throw new Exception("Local storage path cannot be null");
		var path     = Path.Combine(pathBase, filename);

		await using var writer = File.OpenWrite(path);
		stream.Seek(0, SeekOrigin.Begin);
		await stream.CopyToAsync(writer);
		return $"https://{instanceConfig.Value.WebDomain}/files/{filename}";
	}

	private async Task<string> StoreFileVersionObjectStorageAsync(
		Stream stream, string accessKey, string filename, string mimeType
	)
	{
		stream.Seek(0, SeekOrigin.Begin);
		await storageSvc.UploadFileAsync(accessKey, mimeType, filename, stream);
		return storageSvc.GetFilePublicUrl(accessKey).AbsoluteUri;
	}

	public async Task RemoveFileAsync(DriveFile file)
	{
		await RemoveFileAsync(file.Id);
	}

	public async Task RemoveFileAsync(string fileId)
	{
		var job = new DriveFileDeleteJobData { DriveFileId = fileId, Expire = false };
		await queueSvc.BackgroundTaskQueue.EnqueueAsync(job);
	}

	public async Task ExpireFileAsync(DriveFile file, CancellationToken token = default)
	{
		if (file is not { UserHost: not null, Uri: not null, IsLink: false }) return;

		string?[] paths          = [file.AccessKey, file.ThumbnailAccessKey, file.PublicAccessKey];
		var       storedInternal = file.StoredInternal;

		file.IsLink             = true;
		file.Url                = file.Uri;
		file.ThumbnailUrl       = null;
		file.PublicUrl          = null;
		file.ThumbnailAccessKey = null;
		file.PublicAccessKey    = null;
		file.ThumbnailMimeType  = null;
		file.PublicMimeType     = null;
		file.StoredInternal     = false;

		await db.SaveChangesAsync(token);

		var deduplicated =
			await db.DriveFiles.AnyAsync(p => p.Id != file.Id && p.AccessKey == file.AccessKey && !p.IsLink, token);

		if (deduplicated)
			return;

		if (storedInternal)
		{
			var pathBase = storageConfig.Value.Local?.Path
			               ?? throw new Exception("Cannot delete locally stored file: pathBase is null");

			paths.Where(p => p != null)
			     .Select(p => Path.Combine(pathBase, p!))
			     .Where(File.Exists)
			     .ToList()
			     .ForEach(File.Delete);
		}
		else
		{
			await storageSvc.RemoveFilesAsync(paths.Where(p => p != null).Select(p => p!).ToArray());
		}
	}

	public async Task<HashSet<string>> GetAllFileNamesFromObjectStorageAsync()
	{
		return storageConfig.Value.ObjectStorage?.Bucket != null
			? await storageSvc.EnumerateFiles().ToArrayAsync().AsTask().ContinueWithResult(p => p.ToHashSet())
			: [];
	}

	public HashSet<string> GetAllFileNamesFromLocalStorage()
	{
		return storageConfig.Value.Local?.Path is { } path && Directory.Exists(path)
			? Directory.EnumerateFiles(path).Select(Path.GetFileName).NotNull().ToHashSet()
			: [];
	}

	public static bool VerifyFileExistence(
		DriveFile file, HashSet<string> objectStorageFiles, HashSet<string> localStorageFiles,
		out bool original, out bool thumbnail, out bool @public
	)
	{
		string?[] allFilenames = [file.AccessKey, file.ThumbnailAccessKey, file.PublicAccessKey];
		var       filenames    = allFilenames.NotNull().ToArray();
		var missing = file.StoredInternal
			? filenames.Where(p => !localStorageFiles.Contains(p)).ToArray()
			: filenames.Where(p => !objectStorageFiles.Contains(p)).ToArray();

		original  = !missing.Contains(file.AccessKey);
		thumbnail = file.ThumbnailAccessKey == null || !missing.Contains(file.ThumbnailAccessKey);
		@public   = file.PublicAccessKey == null || !missing.Contains(file.PublicAccessKey);

		return original && thumbnail && @public;
	}

	private static string GenerateDerivedFileName(string filename, string newExt)
	{
		return filename.EndsWith($".{newExt}") ? filename : $"{filename}.{newExt}";
	}

	private static string GenerateAccessKey(string prefix = "", string extension = "webp")
	{
		var guid = Guid.NewGuid().ToStringLower();
		return extension.Length > 0 ? $"{prefix}-{guid}.{extension}" : $"{prefix}-{guid}";
	}

	private static string CleanMimeType(string? mimeType)
	{
		return mimeType == null || !Constants.BrowserSafeMimeTypes.Contains(mimeType)
			? "application/octet-stream"
			: mimeType;
	}

	private IReadOnlyCollection<ImageVersion> GetFormats(
		User user, DriveFileCreationRequest request, bool skipImageProcessing
	)
	{
		if (skipImageProcessing)
		{
			var origFormat = new ImageFormat.Keep(Path.GetExtension(request.Filename).TrimStart('.'), request.MimeType);
			return [new ImageVersion(KeyEnum.Original, origFormat)];
		}

		return Enum.GetValues<KeyEnum>()
		           .ToDictionary(p => p, p => GetFormatFromConfig(request, user, p))
		           .Where(p => p.Value != null)
		           .Select(p => new ImageVersion(p.Key, p.Value!))
		           .ToImmutableArray()
		           .AsReadOnly();
	}

	private ImageFormat? GetFormatFromConfig(DriveFileCreationRequest request, User user, KeyEnum key)
	{
		var ver = key switch
		{
			KeyEnum.Original  => storageConfig.Value.MediaProcessing.ImagePipeline.Original,
			KeyEnum.Thumbnail => storageConfig.Value.MediaProcessing.ImagePipeline.Thumbnail,
			KeyEnum.Public    => storageConfig.Value.MediaProcessing.ImagePipeline.Public,
			_                 => throw new ArgumentOutOfRangeException()
		};
		var config = user.IsLocalUser ? ver.Local : ver.Remote;

		// @formatter:off
		return config.Format switch
		{
			ImageFormatEnum.None => null,
			ImageFormatEnum.Keep => new ImageFormat.Keep(Path.GetExtension(request.Filename).TrimStart('.'), request.MimeType),
			ImageFormatEnum.Webp => new ImageFormat.Webp(config.WebpCompressionMode, GetQualityFactor(), GetTargetRes()),
			ImageFormatEnum.Avif => new ImageFormat.Avif(config.AvifCompressionMode, GetQualityFactor(), config.AvifBitDepth, GetTargetRes()),
			ImageFormatEnum.Jxl  => new ImageFormat.Jxl(config.JxlCompressionMode, GetQualityFactor(), config.JxlEffort, GetTargetRes()),
			_                    => throw new ArgumentOutOfRangeException()
		};

		int GetQualityFactor() => request.MimeType == "image/png" ? config.QualityFactorPngSource : config.QualityFactor;
		int GetTargetRes() => config.TargetRes ?? throw new Exception("TargetRes is required to encode images");
		// @formatter:on
	}

	/// <summary>
	///     We can't trust the Content-Length header, and it might be null.
	///     This makes sure that we only ever read up to maxLength into memory.
	/// </summary>
	/// <param name="stream">The response content stream</param>
	/// <param name="maxLength">The maximum length to buffer (null = unlimited)</param>
	/// <param name="contentLength">The content length, if known</param>
	/// <param name="token">A CancellationToken, if applicable</param>
	/// <returns>Either a buffered MemoryStream, or Stream.Null</returns>
	private static async Task<Stream> GetSafeStreamOrNullAsync(
		Stream stream, long? maxLength, long? contentLength, CancellationToken token = default
	)
	{
		if (maxLength is 0) return Stream.Null;
		if (contentLength > maxLength) return Stream.Null;

		MemoryStream buf = new();
		if (contentLength < maxLength)
			maxLength = contentLength.Value;

		await stream.CopyToAsync(buf, maxLength, token);
		if (maxLength == null || buf.Length <= maxLength)
		{
			buf.Seek(0, SeekOrigin.Begin);
			return buf;
		}

		await buf.DisposeAsync();
		return Stream.Null;
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

file static class DriveFileExtensions
{
	public static DriveFile Clone(this DriveFile file, User user, DriveFileCreationRequest request)
	{
		if (file.IsLink)
			throw new Exception("Refusing to clone remote file");

		return new DriveFile
		{
			Id                 = IdHelpers.GenerateSnowflakeId(),
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
			ThumbnailUrl       = file.ThumbnailUrl,
			IsSensitive        = request.IsSensitive,
			PublicMimeType     = file.PublicMimeType,
			PublicUrl          = file.PublicUrl,
			PublicAccessKey    = file.PublicAccessKey,
			StoredInternal     = file.StoredInternal,
			UserHost           = user.Host,
			Comment            = request.Comment,
			RequestHeaders     = request.RequestHeaders,
			RequestIp          = request.RequestIp,
			ThumbnailAccessKey = file.ThumbnailAccessKey
		};
	}
}
