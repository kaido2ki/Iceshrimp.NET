using System.Diagnostics.CodeAnalysis;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Federation.Cryptography;
using Iceshrimp.Backend.Core.Middleware;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Core.Services;

public class DriveService(
	DatabaseContext db,
	ObjectStorageService storageSvc,
	[SuppressMessage("ReSharper", "SuggestBaseTypeForParameterInConstructor")]
	IOptionsSnapshot<Config.StorageSection> storageConfig,
	IOptions<Config.InstanceSection> instanceConfig
) {
	public async Task<DriveFile> StoreFile(Stream data, User user, FileCreationRequest request) {
		var buf    = new BufferedStream(data);
		var digest = await DigestHelpers.Sha256DigestAsync(buf);
		var file   = await db.DriveFiles.FirstOrDefaultAsync(p => p.Sha256 == digest);
		if (file is { IsLink: true }) {
			if (file.UserId == user.Id)
				return file;

			var clonedFile = file.Clone(user, request);
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
			User     = user,
			UserHost = user.Host,
			//Blurhash           = TODO,
			Sha256     = digest,
			Size       = (int)buf.Length,
			//Properties         = TODO,
			IsLink     = !shouldStore && user.Host != null,
			AccessKey  = filename,
			//ThumbnailUrl       = TODO,
			//ThumbnailAccessKey = TODO,
			IsSensitive = request.IsSensitive,
			//WebpublicType      = TODO,
			//WebpublicUrl       = TODO,
			//WebpublicAccessKey = TODO,
			StoredInternal = storedInternal,
			Src            = request.Source,
			Uri            = request.Uri,
			Url            = url,
			Name           = request.Filename,
			Comment        = request.Comment,
			Type           = request.MimeType,
			RequestHeaders = request.RequestHeaders,
			RequestIp      = request.RequestIp
		};

		await db.AddAsync(file);
		await db.SaveChangesAsync();

		return file;
	}

	private static (string filename, string guid) GenerateFilenameKeepingExtension(string filename) {
		var guid = Guid.NewGuid().ToString().ToLowerInvariant();
		var ext  = Path.GetExtension(filename);
		return (guid + ext, guid);
	}

	//TODO: for delete, only delete from object/local storage if no files reference the file anymore
}

public class FileCreationRequest {
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
	public static DriveFile Clone(this DriveFile file, User user, FileCreationRequest request) {
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