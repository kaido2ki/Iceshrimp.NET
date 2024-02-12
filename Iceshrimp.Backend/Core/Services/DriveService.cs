using System.Diagnostics.CodeAnalysis;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Federation.Cryptography;
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
	ILogger<DriveService> logger
) {
	public async Task<DriveFile?> StoreFile(string? uri, User user, bool sensitive) {
		if (uri == null) return null;

		logger.LogDebug("Storing file {uri} for user {userId}", uri, user.Id);

		try {
			// Do we already have the file?
			var file = await db.DriveFiles.FirstOrDefaultAsync(p => p.Uri == uri);
			if (file != null) {
				// If the user matches, return the existing file
				if (file.UserId == user.Id) {
					logger.LogDebug("File {uri} is already registered for user, returning existing file {id}",
					                uri, file.Id);
					return file;
				}

				// Otherwise, clone the file
				var req = new DriveFileCreationRequest {
					Uri         = uri,
					IsSensitive = sensitive,
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

			var request = new DriveFileCreationRequest {
				Uri         = uri,
				Filename    = new Uri(uri).AbsolutePath.Split('/').LastOrDefault() ?? "",
				IsSensitive = sensitive,
				MimeType    = CleanMimeType(res.Content.Headers.ContentType?.MediaType)
			};

			return await StoreFile(await res.Content.ReadAsStreamAsync(), user, request);
		}
		catch (Exception e) {
			logger.LogError("Failed to insert file {uri}: {error}", uri, e.Message);
			return null;
		}
	}

	public async Task<DriveFile> StoreFile(Stream data, User user, DriveFileCreationRequest request) {
		var buf    = new BufferedStream(data);
		var digest = await DigestHelpers.Sha256DigestAsync(buf);
		logger.LogDebug("Storing file {digest} for user {userId}", digest, user.Id);
		var file = await db.DriveFiles.FirstOrDefaultAsync(p => p.Sha256 == digest);
		if (file is { IsLink: false }) {
			if (file.UserId == user.Id) {
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
		var (filename, guid) = GenerateFilenameKeepingExtension(request.Filename);
		var shouldStore    = storageConfig.Value.MediaRetention != null || user.Host == null;
		var storedInternal = storageConfig.Value.Mode == Enums.FileStorage.Local;

		string url;

		if (request.Uri == null && user.Host != null)
			throw GracefulException.UnprocessableEntity("Refusing to store file without uri for remote user");

		if (shouldStore) {
			if (storedInternal) {
				var pathBase = storageConfig.Value.Local?.Path ??
				               throw new Exception("Local storage path cannot be null");
				var path = Path.Combine(pathBase, filename);

				await using var writer = File.OpenWrite(path);
				await buf.CopyToAsync(writer);
				url = $"https://{instanceConfig.Value.WebDomain}/files/{filename}";
			}
			else {
				await storageSvc.UploadFileAsync(filename, data);
				url = storageSvc.GetFilePublicUrl(filename).AbsoluteUri;
			}
		}
		else {
			url = request.Uri ?? throw new Exception("Uri must not be null at this stage");
		}

		file = new DriveFile {
			User           = user,
			UserHost       = user.Host,
			Sha256         = digest,
			Size           = (int)buf.Length,
			IsLink         = !shouldStore && user.Host != null,
			AccessKey      = filename,
			IsSensitive    = request.IsSensitive,
			StoredInternal = storedInternal,
			Src            = request.Source,
			Uri            = request.Uri,
			Url            = url,
			Name           = request.Filename,
			Comment        = request.Comment,
			Type           = request.MimeType,
			RequestHeaders = request.RequestHeaders,
			RequestIp      = request.RequestIp
			//Blurhash           = TODO,
			//Properties         = TODO,
			//ThumbnailUrl       = TODO,
			//ThumbnailAccessKey = TODO,
			//WebpublicType      = TODO,
			//WebpublicUrl       = TODO,
			//WebpublicAccessKey = TODO,
		};

		await db.AddAsync(file);
		await db.SaveChangesAsync();

		return file;
	}

	public async Task RemoveFile(DriveFile file) {
		await RemoveFile(file.Id);
	}

	public async Task RemoveFile(string fileId) {
		var job = new DriveFileDeleteJob { DriveFileId = fileId };
		await queueSvc.BackgroundTaskQueue.EnqueueAsync(job);
	}

	public string GetPublicUrl(DriveFile file, bool thumbnail) {
		return thumbnail
			? file.ThumbnailUrl ?? file.WebpublicUrl ?? file.Url
			: file.WebpublicUrl ?? file.Url;
	}

	private static (string filename, string guid) GenerateFilenameKeepingExtension(string filename) {
		var guid = Guid.NewGuid().ToString().ToLowerInvariant();
		var ext  = Path.GetExtension(filename);
		return (guid + ext, guid);
	}

	private static string CleanMimeType(string? mimeType) {
		return mimeType == null || !Constants.BrowserSafeMimeTypes.Contains(mimeType)
			? "application/octet-stream"
			: mimeType;
	}
}

public class DriveFileCreationRequest {
	public          string?                     Comment;
	public required string                      Filename = Guid.NewGuid().ToString().ToLowerInvariant();
	public required bool                        IsSensitive;
	public required string                      MimeType;
	public          Dictionary<string, string>? RequestHeaders;
	public          string?                     RequestIp;
	public          string?                     Source;
	public          string?                     Uri;
}

//TODO: set uri as well (which may be different)
file static class DriveFileExtensions {
	public static DriveFile Clone(this DriveFile file, User user, DriveFileCreationRequest request) {
		if (file.IsLink)
			throw new Exception("Refusing to clone remote file");

		return new DriveFile {
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