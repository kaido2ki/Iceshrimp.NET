using System.Collections.Immutable;
using System.Text;
using Amazon;
using Amazon.S3;
using Carbon.Storage;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Helpers;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Core.Services;

public class ObjectStorageService(IOptions<Config.StorageSection> config, HttpClient httpClient) {
	private readonly string? _accessUrl = config.Value.ObjectStorage?.AccessUrl;

	private readonly S3Bucket? _bucket = GetBucketSafely(config);

	private readonly string? _prefix = config.Value.ObjectStorage?.Prefix?.Trim('/');

	private static S3Bucket? GetBucketSafely(IOptions<Config.StorageSection> config) {
		if (config.Value.Mode != Enums.FileStorage.Local) return GetBucket(config);

		try {
			return GetBucket(config);
		}
		catch {
			return null;
		}
	}

	private static S3Bucket GetBucket(IOptions<Config.StorageSection> config) {
		var s3Config = config.Value.ObjectStorage ?? throw new Exception("Invalid object storage configuration");

		var region    = s3Config.Region ?? throw new Exception("Invalid object storage region");
		var endpoint  = s3Config.Endpoint ?? throw new Exception("Invalid object storage endpoint");
		var accessKey = s3Config.KeyId ?? throw new Exception("Invalid object storage access key");
		var secretKey = s3Config.SecretKey ?? throw new Exception("Invalid object storage secret key");
		var bucket    = s3Config.Bucket ?? throw new Exception("Invalid object storage bucket");

		if (config.Value.ObjectStorage?.AccessUrl == null)
			throw new Exception("Invalid object storage access url");

		var client = new S3Client(new AwsRegion(region), endpoint, new AwsCredential(accessKey, secretKey));
		return new S3Bucket(bucket, client);
	}

	public async Task VerifyCredentialsAsync() {
		const string filename = ".iceshrimp-test";
		var          content  = CryptographyHelpers.GenerateRandomString(16);

		await UploadFileAsync(filename, Encoding.UTF8.GetBytes(content));

		string result;
		try {
			result = await httpClient.GetStringAsync(GetFilePublicUrl(filename));
		}
		catch (Exception e) {
			throw new Exception($"Failed to verify access url: {e.Message}");
		}

		if (result == content)
			return;
		throw new Exception("Failed to verify access url (content mismatch)");
	}

	public async Task UploadFileAsync(string filename, byte[] data) {
		if (_bucket == null) throw new Exception("Refusing to upload to object storage with invalid configuration");
		await _bucket.PutAsync(new Blob(GetFilenameWithPrefix(filename), data));
	}

	public async Task UploadFileAsync(string filename, Stream data) {
		if (_bucket == null) throw new Exception("Refusing to upload to object storage with invalid configuration");
		await _bucket.PutAsync(new Blob(GetFilenameWithPrefix(filename), data));
	}

	public async Task<string> UploadFileAsync(byte[] data) {
		if (_bucket == null) throw new Exception("Refusing to upload to object storage with invalid configuration");
		var filename = Guid.NewGuid().ToString().ToLowerInvariant();
		await _bucket.PutAsync(new Blob(GetFilenameWithPrefix(filename), data));
		return filename;
	}

	public Uri GetFilePublicUrl(string filename) {
		var baseUri = new Uri(_accessUrl ?? throw new Exception("Invalid object storage access url"));
		return new Uri(baseUri, GetFilenameWithPrefix(filename));
	}

	public async ValueTask<Stream?> GetFileAsync(string filename) {
		if (_bucket == null) throw new Exception("Refusing to get file from object storage with invalid configuration");

		try {
			var res = await _bucket.GetAsync(GetFilenameWithPrefix(filename));
			return await res.OpenAsync();
		}
		catch {
			return null;
		}
	}

	public async Task RemoveFilesAsync(params string[] filenames) {
		if (_bucket == null)
			throw new Exception("Refusing to remove file from object storage with invalid configuration");
		await _bucket.DeleteAsync(filenames.Select(GetFilenameWithPrefix).ToImmutableList());
	}

	private string GetFilenameWithPrefix(string filename) {
		return !string.IsNullOrWhiteSpace(_prefix) ? _prefix + "/" + filename : filename;
	}
}