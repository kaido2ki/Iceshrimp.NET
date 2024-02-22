using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Iceshrimp.Backend.Core.Middleware;

namespace Iceshrimp.Backend.Core.Configuration;

public sealed class Config
{
	public required InstanceSection Instance { get; init; } = new();
	public required WorkerSection   Worker   { get; init; } = new();
	public required DatabaseSection Database { get; init; } = new();
	public required SecuritySection Security { get; init; } = new();
	public required StorageSection  Storage  { get; init; } = new();

	public sealed class InstanceSection
	{
		public readonly string Version;

		public InstanceSection()
		{
			// Get version information from assembly
			var version = Assembly.GetEntryAssembly()!
			                      .GetCustomAttributes()
			                      .OfType<AssemblyInformationalVersionAttribute>()
			                      .First()
			                      .InformationalVersion;

			// If we have a git revision, limit it to 10 characters
			if (version.Split('+') is { Length: 2 } split)
			{
				split[1] = split[1][..Math.Min(split[1].Length, 10)];
				Version  = string.Join('+', split);
			}
			else
			{
				Version = version;
			}
		}

		public string UserAgent => $"Iceshrimp.NET/{Version} (https://{WebDomain})";

		[Range(1, 65535)] public  int     ListenPort     { get; init; } = 3000;
		[Required]        public  string  ListenHost     { get; init; } = "localhost";
		public                    string? ListenSocket   { get; init; }
		[Required]         public string  WebDomain      { get; init; } = null!;
		[Required]         public string  AccountDomain  { get; init; } = null!;
		[Range(1, 100000)] public int     CharacterLimit { get; init; } = 8192;
	}
	
	public sealed class WorkerSection
	{
		[MaxLength(64)] public string? WorkerId { get; init; }
	}

	public sealed class SecuritySection
	{
		public bool                 AuthorizedFetch      { get; init; } = true;
		public bool                 AttachLdSignatures   { get; init; } = false;
		public bool                 AcceptLdSignatures   { get; init; } = false;
		public ExceptionVerbosity   ExceptionVerbosity   { get; init; } = ExceptionVerbosity.Basic;
		public Enums.Registrations  Registrations        { get; init; } = Enums.Registrations.Closed;
		public Enums.FederationMode FederationMode       { get; init; } = Enums.FederationMode.BlockList;
		public Enums.ItemVisibility ExposeFederationList { get; init; } = Enums.ItemVisibility.Registered;
		public Enums.ItemVisibility ExposeBlockReasons   { get; init; } = Enums.ItemVisibility.Registered;
		public bool                 EnablePublicPreview  { get; init; } = true;
	}

	public sealed class DatabaseSection
	{
		[Required]        public string  Host     { get; init; } = "localhost";
		[Range(1, 65535)] public int     Port     { get; init; } = 5432;
		[Required]        public string  Database { get; init; } = null!;
		[Required]        public string  Username { get; init; } = null!;
		public                   string? Password { get; init; }
	}

	public sealed class StorageSection
	{
		public readonly TimeSpan? MediaRetentionTimeSpan;

		public bool              CleanAvatars = false;
		public bool              CleanBanners = false;
		public Enums.FileStorage Mode { get; init; } = Enums.FileStorage.Local;

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

		public LocalStorageSection?  Local         { get; init; }
		public ObjectStorageSection? ObjectStorage { get; init; }
	}

	public sealed class LocalStorageSection
	{
		public string? Path { get; init; }
	}

	public sealed class ObjectStorageSection
	{
		public string? Endpoint  { get; init; }
		public string? Region    { get; init; }
		public string? KeyId     { get; init; }
		public string? SecretKey { get; init; }
		public string? Bucket    { get; init; }
		public string? Prefix    { get; init; }
		public string? AccessUrl { get; init; }
	}
}