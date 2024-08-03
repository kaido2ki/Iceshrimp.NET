using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Shared.Helpers;

namespace Iceshrimp.Backend.Core.Configuration;

public sealed class Config
{
	public required InstanceSection    Instance    { get; init; } = new();
	public required DatabaseSection    Database    { get; init; } = new();
	public required SecuritySection    Security    { get; init; } = new();
	public required StorageSection     Storage     { get; init; } = new();
	public required PerformanceSection Performance { get; init; } = new();
	public required QueueSection       Queue       { get; init; } = new();

	public sealed class InstanceSection
	{
		private readonly VersionInfo _versionInfo = VersionHelpers.GetVersionInfo();

		public string  Codename   => _versionInfo.Codename;
		public string  Edition    => _versionInfo.Edition;
		public string? CommitHash => _versionInfo.CommitHash;
		public string  RawVersion => _versionInfo.RawVersion;
		public string  Version    => _versionInfo.Version;
		public string  UserAgent  => $"Iceshrimp.NET/{Version} (+https://{WebDomain}/)";

		[Range(1, 65535)] public  int     ListenPort     { get; init; } = 3000;
		[Required]        public  string  ListenHost     { get; init; } = "localhost";
		public                    string? ListenSocket   { get; init; }
		[Required]         public string  WebDomain      { get; init; } = null!;
		[Required]         public string  AccountDomain  { get; init; } = null!;
		[Range(1, 100000)] public int     CharacterLimit { get; init; } = 8192;
	}

	public sealed class SecuritySection
	{
		public bool                 AuthorizedFetch      { get; init; } = true;
		public bool                 AttachLdSignatures   { get; init; } = false;
		public bool                 AcceptLdSignatures   { get; init; } = false;
		public bool                 AllowLoopback        { get; init; } = false;
		public bool                 AllowLocalIPv6       { get; init; } = false;
		public bool                 AllowLocalIPv4       { get; init; } = false;
		public ExceptionVerbosity   ExceptionVerbosity   { get; init; } = ExceptionVerbosity.Basic;
		public Enums.Registrations  Registrations        { get; init; } = Enums.Registrations.Closed;
		public Enums.FederationMode FederationMode       { get; init; } = Enums.FederationMode.BlockList;
		public Enums.ItemVisibility ExposeFederationList { get; init; } = Enums.ItemVisibility.Registered;
		public Enums.ItemVisibility ExposeBlockReasons   { get; init; } = Enums.ItemVisibility.Registered;
		public Enums.PublicPreview  PublicPreview        { get; init; } = Enums.PublicPreview.Public;
	}

	public sealed class DatabaseSection
	{
		[Required]        public string  Host             { get; init; } = "localhost";
		[Range(1, 65535)] public int     Port             { get; init; } = 5432;
		[Required]        public string  Database         { get; init; } = null!;
		[Required]        public string  Username         { get; init; } = null!;
		public                   string? Password         { get; init; }
		[Range(1, 1000)] public  int     MaxConnections   { get; init; } = 100;
		public                   bool    Multiplexing     { get; init; } = true;
		public                   bool    ParameterLogging { get; init; } = false;
	}

	public sealed class StorageSection
	{
		public readonly long?      MaxCacheSizeBytes;
		public readonly long?      MaxUploadSizeBytes;
		public readonly TimeSpan? MediaRetentionTimeSpan;

		public bool              CleanAvatars = false;
		public bool              CleanBanners = false;
		public Enums.FileStorage Provider { get; init; } = Enums.FileStorage.Local;

		[Obsolete("This property is for backwards compatibility only, use StorageSection.Provider instead", true)]
		[SuppressMessage("ReSharper", "UnusedMember.Global")]
		public Enums.FileStorage Mode
		{
			get => Provider;
			init => Provider = value;
		}

		public string? MediaRetention
		{
			get => MediaRetentionTimeSpan?.ToString();
			init
			{
				if (value == null || string.IsNullOrWhiteSpace(value) || value.Trim() == "0")
				{
					MediaRetentionTimeSpan = null;
					return;
				}

				if (value.Trim() == "-1")
				{
					MediaRetentionTimeSpan = TimeSpan.MaxValue;
					return;
				}

				if (value.Length < 2 || !int.TryParse(value[..^1].Trim(), out var num))
					throw new Exception("Invalid media retention time");

				var suffix = value[^1];

				MediaRetentionTimeSpan = suffix switch
				{
					'd' => TimeSpan.FromDays(num),
					'w' => TimeSpan.FromDays(num * 7),
					'm' => TimeSpan.FromDays(num * 30),
					'y' => TimeSpan.FromDays(num * 365),
					_   => throw new Exception("Unsupported suffix, use one of: [d]ays, [w]eeks, [m]onths, [y]ears")
				};
			}
		}

		public string? MaxUploadSize
		{
			get => MaxUploadSizeBytes?.ToString();
			init
			{
				if (value == null || string.IsNullOrWhiteSpace(value) || value.Trim() == "0" || value.Trim() == "-1")
				{
					MaxUploadSizeBytes = null;
					return;
				}

				var hasSuffix = !char.IsAsciiDigit(value.Trim()[^1]);
				var substr    = hasSuffix ? value.Trim()[..^1] : value.Trim();

				if (!int.TryParse(substr, out var num))
					throw new Exception("Invalid max upload size");

				char? suffix = hasSuffix ? value.Trim()[^1] : null;

				MaxUploadSizeBytes = suffix switch
				{
					null       => num,
					'k' or 'K' => num * 1024L,
					'm' or 'M' => num * 1024L * 1024,
					'g' or 'G' => num * 1024L * 1024 * 1024,

					_ => throw new Exception("Unsupported suffix, use one of: [K]ilobytes, [M]egabytes, [G]igabytes")
				};
			}
		}

		public string? MaxCacheSize
		{
			get => MaxCacheSizeBytes?.ToString();
			init
			{
				if (value == null || string.IsNullOrWhiteSpace(value) || value.Trim() == "0" || value.Trim() == "-1")
				{
					MaxCacheSizeBytes = null;
					return;
				}

				var hasSuffix = !char.IsAsciiDigit(value.Trim()[^1]);
				var substr    = hasSuffix ? value.Trim()[..^1] : value.Trim();

				if (!int.TryParse(substr, out var num))
					throw new Exception("Invalid max cache size");

				char? suffix = hasSuffix ? value.Trim()[^1] : null;

				MaxCacheSizeBytes = suffix switch
				{
					null       => num,
					'k' or 'K' => num * 1024L,
					'm' or 'M' => num * 1024L * 1024,
					'g' or 'G' => num * 1024L * 1024 * 1024,

					_ => throw new Exception("Unsupported suffix, use one of: [K]ilobytes, [M]egabytes, [G]igabytes")
				};
			}
		}

		public LocalStorageSection?   Local           { get; init; }
		public ObjectStorageSection?  ObjectStorage   { get; init; }
		public MediaProcessingSection MediaProcessing { get; init; } = new();
	}

	public sealed class LocalStorageSection
	{
		public string? Path { get; init; }
	}

	public sealed class ObjectStorageSection
	{
		public string? Endpoint          { get; init; }
		public string? Region            { get; init; }
		public string? KeyId             { get; init; }
		public string? SecretKey         { get; init; }
		public string? Bucket            { get; init; }
		public string? Prefix            { get; init; }
		public string? AccessUrl         { get; init; }
		public string? SetAcl            { get; init; }
		public bool    DisableValidation { get; init; } = false;
	}

	public sealed class MediaProcessingSection
	{
		public readonly int                  MaxFileSizeBytes = 10 * 1024 * 1024;
		public          Enums.ImageProcessor ImageProcessor       { get; init; } = Enums.ImageProcessor.ImageSharp;
		public          int                  MaxResolutionMpx     { get; init; } = 50;
		public          bool                 LocalOnly            { get; init; } = false;
		public          int                  DefaultImageFormat   { get; init; } = 1;
		public          int                  DefaultImageQuality  { get; init; } = 75;
		public          int                  DefaultImageHeight   { get; init; } = 4096;
		public          int                  DefaultImageWidth    { get; init; } = 4096;
		public          int                  DefaultOriginalImage { get; init; } = 1;

		public string MaxFileSize
		{
			get => MaxFileSizeBytes.ToString();
			init
			{
				if (value == null || string.IsNullOrWhiteSpace(value) || value.Trim() == "0" || value.Trim() == "-1")
				{
					throw new
						Exception("Invalid max file size, to disable media processing set ImageProcessor to 'None'");
				}

				var hasSuffix = !char.IsAsciiDigit(value.Trim()[^1]);
				var substr    = hasSuffix ? value.Trim()[..^1] : value.Trim();

				if (!int.TryParse(substr, out var num))
					throw new Exception("Invalid max file size");

				char? suffix = hasSuffix ? value.Trim()[^1] : null;

				MaxFileSizeBytes = suffix switch
				{
					null       => num,
					'k' or 'K' => num * 1024,
					'm' or 'M' => num * 1024 * 1024,
					'g' or 'G' => num * 1024 * 1024 * 1024,

					_ => throw new Exception("Unsupported suffix, use one of: [K]ilobytes, [M]egabytes, [G]igabytes")
				};
			}
		}
	}

	public sealed class PerformanceSection
	{
		public QueueConcurrencySection QueueConcurrency { get; init; } = new();

		[Range(0, int.MaxValue)] public int FederationRequestHandlerConcurrency { get; init; } = 0;
	}

	public sealed class QueueConcurrencySection
	{
		[Range(1, int.MaxValue)] public int Inbox          { get; init; } = 4;
		[Range(1, int.MaxValue)] public int Deliver        { get; init; } = 20;
		[Range(1, int.MaxValue)] public int PreDeliver     { get; init; } = 4;
		[Range(1, int.MaxValue)] public int BackgroundTask { get; init; } = 4;
	}

	public sealed class QueueSection
	{
		public JobRetentionSection JobRetention { get; init; } = new();
	}

	public sealed class JobRetentionSection
	{
		[Range(0, int.MaxValue)] public int Completed { get; init; } = 100;
		[Range(0, int.MaxValue)] public int Failed    { get; init; } = 10;
	}
}