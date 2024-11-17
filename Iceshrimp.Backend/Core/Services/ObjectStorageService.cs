using System.Collections.Immutable;
using System.Net.Http.Headers;
using System.Text;
using Carbon.Storage;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Helpers;
using Iceshrimp.ObjectStorage.Core.Models;
using Iceshrimp.ObjectStorage.Core.Security;
using Iceshrimp.ObjectStorage.S3.Client;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Core.Services;

public class ObjectStorageService(IOptions<Config.StorageSection> config, HttpClient httpClient)
{
	private readonly string? _accessUrl = config.Value.ObjectStorage?.AccessUrl;

	private readonly IReadOnlyDictionary<string, string>? _acl = config.Value.ObjectStorage?.SetAcl != null
		? new Dictionary<string, string> { ["x-amz-acl"] = config.Value.ObjectStorage.SetAcl }.AsReadOnly()
		: null;

	private readonly S3Bucket? _bucket = GetBucketSafely(config);

	private readonly string? _prefix = config.Value.ObjectStorage?.Prefix?.Trim('/');

	private static S3Bucket? GetBucketSafely(IOptions<Config.StorageSection> config)
	{
		if (config.Value.Provider != Enums.FileStorage.Local) return GetBucket(config);

		try
		{
			return GetBucket(config);
		}
		catch
		{
			return null;
		}
	}

	private static S3Bucket GetBucket(IOptions<Config.StorageSection> config)
	{
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

	public async Task VerifyCredentialsAsync()
	{
		if (config.Value.ObjectStorage?.DisableValidation ?? false)
			return;

		const string filename = ".iceshrimp-test";
		var          content  = CryptographyHelpers.GenerateRandomString(16);

		await UploadFileAsync(filename, "text/plain", filename, Encoding.UTF8.GetBytes(content));

		string result;
		try
		{
			result = await httpClient.GetStringAsync(GetFilePublicUrl(filename));
		}
		catch (Exception e)
		{
			throw new Exception($"Failed to verify access url: {e.Message}");
		}

		if (result == content)
			return;
		throw new Exception("Failed to verify access url (content mismatch)");
	}

	private Task UploadFileAsync(string key, string contentType, string filename, byte[] data) =>
		UploadFileAsync(key, contentType, filename, new MemoryStream(data));

	public async Task UploadFileAsync(string key, string contentType, string filename, Stream data)
	{
		if (_bucket == null) throw new Exception("Refusing to upload to object storage with invalid configuration");

		var properties         = (_acl ?? BlobProperties.Empty).ToDictionary();
		var contentDisposition = new ContentDispositionHeaderValue("inline") { FileName = filename }.ToString();

		properties.Add("Content-Type", contentType);
		properties.Add("Content-Disposition", contentDisposition);

		IBlob blob = data.Length > 0
			? new Blob(GetKeyWithPrefix(key), data, properties)
			: new EmptyBlob(GetKeyWithPrefix(key), data, properties);

		await _bucket.PutAsync(blob);
	}

	public Uri GetFilePublicUrl(string filename)
	{
		var baseUri = new Uri(_accessUrl ?? throw new Exception("Invalid object storage access url"));
		return new Uri(baseUri, GetKeyWithPrefix(filename));
	}

	public async ValueTask<Stream?> GetFileAsync(string filename)
	{
		if (_bucket == null) throw new Exception("Refusing to get file from object storage with invalid configuration");

		try
		{
			var res = await _bucket.GetAsync(GetKeyWithPrefix(filename));
			return await res.OpenAsync();
		}
		catch
		{
			return null;
		}
	}

	public async Task RemoveFilesAsync(params string[] filenames)
	{
		if (_bucket == null)
			throw new Exception("Refusing to remove file from object storage with invalid configuration");
		await _bucket.DeleteAsync(filenames.Select(GetKeyWithPrefix).ToImmutableList());
	}

	public async IAsyncEnumerable<string> EnumerateFilesAsync()
	{
		if (_bucket == null)
			throw new Exception("Refusing to enumerate files from object storage with invalid configuration");
		var prefix       = _prefix != null ? _prefix + "/" : null;
		var prefixLength = prefix?.Length;
		await foreach (var blob in _bucket.ScanAsync(prefix))
			yield return prefixLength != null ? blob.Key[prefixLength.Value..] : blob.Key;
	}

	private string GetKeyWithPrefix(string filename)
	{
		return !string.IsNullOrWhiteSpace(_prefix) ? _prefix + "/" + filename : filename;
	}

	private class EmptyBlob(string key, Stream stream, IReadOnlyDictionary<string, string> properties) : IBlob
	{
		private bool _isDisposed;

		public void Dispose()
		{
			if (_isDisposed) return;
			stream.Dispose();
			_isDisposed = true;
		}

		public ValueTask<Stream> OpenAsync() => ValueTask.FromResult(stream);

		public string                              Key        => key;
		public long                                Size       => 0;
		public DateTime                            Modified   => DateTime.UtcNow;
		public IReadOnlyDictionary<string, string> Properties => properties;
	}
}