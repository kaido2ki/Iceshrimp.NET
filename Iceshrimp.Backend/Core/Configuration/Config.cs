using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Backend.Core.Services.ImageProcessing;
using Iceshrimp.Shared.Helpers;

namespace Iceshrimp.Backend.Core.Configuration;

[SuppressMessage("ReSharper", "UnusedMember.Local")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
[SuppressMessage("ReSharper", "AutoPropertyCanBeMadeGetOnly.Global")]
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
[SuppressMessage("ReSharper", "RedundantDefaultMemberInitializer")]
[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class Config
{
	public required InstanceSection    Instance    { get; init; } = new();
	public required DatabaseSection    Database    { get; init; } = new();
	public required SecuritySection    Security    { get; init; } = new();
	public required StorageSection     Storage     { get; init; } = new();
	public required PerformanceSection Performance { get; init; } = new();
	public required QueueSection       Queue       { get; init; } = new();
	public required BackfillSection    Backfill    { get; init; } = new();

	public sealed class InstanceSection
	{
		private readonly VersionInfo _versionInfo = VersionHelpers.VersionInfo.Value;

		public string  Codename   => _versionInfo.Codename;
		public string  Edition    => _versionInfo.Edition;
		public string? CommitHash => _versionInfo.CommitHash;
		public string  RawVersion => _versionInfo.RawVersion;
		public string  Version    => _versionInfo.Version;
		public string  UserAgent  => $"Iceshrimp.NET/{Version} (+https://{WebDomain}/)";

		[Range(1, 65535)] public  int     ListenPort        { get; init; } = 3000;
		[Required]        public  string  ListenHost        { get; init; } = "localhost";
		public                    string? ListenSocket      { get; init; }
		public                    string  ListenSocketPerms { get; init; } = "660";
		[Required]         public string  WebDomain         { get; init; } = null!;
		[Required]         public string  AccountDomain     { get; init; } = null!;
		[Range(1, 100000)] public int     CharacterLimit    { get; init; } = 8192;
		public                    string? RedirectIndexTo   { get; init; }

		public string? AdditionalDomains
		{
			get => string.Join(',', AdditionalDomainsArray);
			init => AdditionalDomainsArray = value?.Split(',').Select(p => p.Trim()).ToArray() ?? [];
		}

		public string[] AdditionalDomainsArray { get; private init; } = [];
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
		public                   bool    Multiplexing     { get; set; }  = true;
		public                   bool    ParameterLogging { get; init; } = false;
	}

	public sealed class StorageSection
	{
		public readonly long?     MaxCacheSizeBytes;
		public readonly long?     MaxUploadSizeBytes;
		public readonly TimeSpan? MediaRetentionTimeSpan;

		public bool              CleanAvatars     { get; init; } = false;
		public bool              CleanBanners     { get; init; } = false;
		public bool              ProxyRemoteMedia { get; init; } = true;
		public Enums.FileStorage Provider         { get; init; } = Enums.FileStorage.Local;

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
			init => MediaRetentionTimeSpan = ParseNaturalDuration(value, "media retention time");
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

	public sealed class MediaProcessingSection : IValidatableObject
	{
		public ImagePipelineSection ImagePipeline { get; init; } = new();

		public readonly int                  MaxFileSizeBytes = 10 * 1024 * 1024;
		public          Enums.ImageProcessor ImageProcessor           { get; init; } = Enums.ImageProcessor.ImageSharp;
		public          int                  MaxResolutionMpx         { get; init; } = 30;
		public          bool                 LocalOnly                { get; init; } = false;
		public          bool                 FailIfImageExceedsMaxRes { get; init; } = false;

		[Range(0, 128)] public int ImageProcessorConcurrency { get; init; } = 8;

		public int MaxResolutionPx => MaxResolutionMpx * 1000 * 1000;

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

		public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
		{
			List<ValidationResult> res = [];
			if (ImageProcessor == Enums.ImageProcessor.None) return res;

			List<ImageFormatConfiguration> formats =
			[
				ImagePipeline.Thumbnail.Local,
				ImagePipeline.Thumbnail.Remote,
				ImagePipeline.Original.Local,
				ImagePipeline.Original.Remote,
				ImagePipeline.Public.Local,
				ImagePipeline.Public.Remote
			];

			if (ImageProcessor == Enums.ImageProcessor.ImageSharp)
			{
				// @formatter:off
				if (formats.Any(p => p.Format is ImageFormatEnum.Avif or ImageFormatEnum.Jxl))
					return [new ValidationResult("ImageSharp does not support AVIF or JXL. Please choose a different format, or switch to LibVips.")];
				// @formatter:on
			}

			// @formatter:off
			if (ImagePipeline.Original.Local.Format == ImageFormatEnum.None || ImagePipeline.Original.Remote.Format == ImageFormatEnum.None)
				return [new ValidationResult("The image format 'None' is not valid for original image versions. Please choose a different format.")];
			// @formatter:on

			formats.ForEach(p =>
			{
				var context = new ValidationContext(p);
				Validator.TryValidateObject(p, context, res, true);
			});

			return res;
		}
	}

	public sealed class ImagePipelineSection
	{
		public ImageVersion Original { get; init; } = new()
		{
			Local  = new ImageFormatConfiguration { Format = ImageFormatEnum.Keep },
			Remote = new ImageFormatConfiguration { Format = ImageFormatEnum.Keep }
		};

		public ImageVersion Thumbnail { get; init; } = new()
		{
			Local = new ImageFormatConfiguration { Format = ImageFormatEnum.Webp, TargetRes = 1000 },
			Remote = new ImageFormatConfiguration
			{
				Format                 = ImageFormatEnum.Webp,
				TargetRes              = 1000,
				QualityFactorPngSource = 75
			}
		};

		public ImageVersion Public { get; init; } = new()
		{
			Local  = new ImageFormatConfiguration { Format = ImageFormatEnum.Webp, TargetRes = 2048 },
			Remote = new ImageFormatConfiguration { Format = ImageFormatEnum.None }
		};
	}

	public class ImageVersion
	{
		[Required] public required ImageFormatConfiguration Local  { get; init; }
		[Required] public required ImageFormatConfiguration Remote { get; init; }
	}

	public class ImageFormatConfiguration
	{
		[Required] public required ImageFormatEnum Format { get; init; }

		[Range(1, 100)]   public int  QualityFactor          { get; init; } = 75;
		[Range(1, 100)]   public int  QualityFactorPngSource { get; init; } = 100;
		[Range(1, 10240)] public int? TargetRes              { get; init; }

		public ImageFormat.Webp.Compression WebpCompressionMode { get; init; } = ImageFormat.Webp.Compression.Lossy;
		public ImageFormat.Avif.Compression AvifCompressionMode { get; init; } = ImageFormat.Avif.Compression.Lossy;
		public ImageFormat.Jxl.Compression  JxlCompressionMode  { get; init; } = ImageFormat.Jxl.Compression.Lossy;

		[Range(8, 12)] public int? AvifBitDepth { get; init; }
		[Range(1, 9)]  public int  JxlEffort    { get; init; } = 7;
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
		[Range(1, int.MaxValue)] public int Backfill       { get; init; } = 10;
		[Range(1, int.MaxValue)] public int BackfillUser   { get; init; } = 10;
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

	public sealed class BackfillSection
	{
		public BackfillRepliesSection Replies { get; init; } = new();
		public BackfillUserSection    User    { get; init; } = new();
	}

	public sealed class BackfillRepliesSection
	{
		public bool Enabled     { get; init; } = false;
		public bool FetchAsUser { get; init; } = false;

		public string? NewNoteDelay
		{
			get => NewNoteDelayTimeSpan.ToString();
			init => NewNoteDelayTimeSpan =
				ParseNaturalDuration(value, "new note delay") ?? TimeSpan.FromMinutes(5);
		}

		public string? RefreshAfter
		{
			get => RefreshAfterTimeSpan.ToString();
			init => RefreshAfterTimeSpan =
				ParseNaturalDuration(value, "refresh renote after duration") ?? TimeSpan.FromMinutes(15);
		}

		public TimeSpan NewNoteDelayTimeSpan = TimeSpan.FromMinutes(5);
		public TimeSpan RefreshAfterTimeSpan = TimeSpan.FromMinutes(15);
	}

	public sealed class BackfillUserSection
	{
		public bool Enabled { get; init; } = false;

		[Range(0, int.MaxValue)] public int MaxItems { get; init; } = 100;

		public string? RefreshAfter
		{
			get => RefreshAfterTimeSpan.ToString();
			init => RefreshAfterTimeSpan =
				ParseNaturalDuration(value, "refresh outbox after duration") ?? TimeSpan.FromDays(30);
		}

		public TimeSpan RefreshAfterTimeSpan = TimeSpan.FromDays(30);
	}

	private static readonly char[] Digits = [..Enumerable.Range(0, 10).Select(p => p.ToString()[0])];

	private static TimeSpan? ParseNaturalDuration(string? value, string name)
	{
		value = value?.Trim();
		if (value == null || string.IsNullOrWhiteSpace(value) || value == "0")
			return null;

		if (value == "-1")
			return TimeSpan.MaxValue;

		var idx = value.LastIndexOfAny(Digits);

		if (!char.IsDigit(value[0]) || idx < 0 || value.Length < 2 || !int.TryParse(value[..++idx], out var num))
			throw new Exception($"Invalid {name}");

		if (num == 0)
			return null;

		var suffix = value[idx..];

		return suffix switch
		{
			"s" => TimeSpan.FromSeconds(num),
			"m" => TimeSpan.FromMinutes(num),
			"h" => TimeSpan.FromHours(num),
			"d" => TimeSpan.FromDays(num),
			_   => throw new Exception("Unsupported suffix, use one of: [s]econds, [m]inutes, [h]ours, [d]ays, [w]eeks")
		};
	}
}