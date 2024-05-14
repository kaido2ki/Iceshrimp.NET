using System.Collections.Immutable;
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

	private readonly S3Bucket? _bucket = GetBucketSafely(config);

	private readonly string? _prefix = config.Value.ObjectStorage?.Prefix?.Trim('/');

	private readonly IReadOnlyDictionary<string, string>? _acl = config.Value.ObjectStorage?.SetAcl != null
		? new Dictionary<string, string> { { "x-amz-acl", config.Value.ObjectStorage.SetAcl } }.AsReadOnly()
		: null;

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

		await UploadFileAsync(filename, "text/plain", Encoding.UTF8.GetBytes(content));

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

	private Task UploadFileAsync(string filename, string contentType, byte[] data) =>
		UploadFileAsync(filename, contentType, new MemoryStream(data));

	public async Task UploadFileAsync(string filename, string contentType, Stream data)
	{
		if (_bucket == null) throw new Exception("Refusing to upload to object storage with invalid configuration");
		var properties = (_acl ?? BlobProperties.Empty).ToDictionary();
		properties.Add("Content-Type", contentType);
		IBlob blob = data.Length > 0
			? new Blob(GetFilenameWithPrefix(filename), data, properties)
			: new EmptyBlob(GetFilenameWithPrefix(filename), data, properties);

		await _bucket.PutAsync(blob);
	}

	public Uri GetFilePublicUrl(string filename)
	{
		var baseUri = new Uri(_accessUrl ?? throw new Exception("Invalid object storage access url"));
		return new Uri(baseUri, GetFilenameWithPrefix(filename));
	}

	public async ValueTask<Stream?> GetFileAsync(string filename)
	{
		if (_bucket == null) throw new Exception("Refusing to get file from object storage with invalid configuration");

		try
		{
			var res = await _bucket.GetAsync(GetFilenameWithPrefix(filename));
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
		await _bucket.DeleteAsync(filenames.Select(GetFilenameWithPrefix).ToImmutableList());
	}

	private string GetFilenameWithPrefix(string filename)
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