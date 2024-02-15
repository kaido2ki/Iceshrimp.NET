using System.Reflection;
using Iceshrimp.Backend.Core.Middleware;

namespace Iceshrimp.Backend.Core.Configuration;

public sealed class Config {
	public required InstanceSection Instance { get; init; } = new();
	public required DatabaseSection Database { get; init; } = new();
	public required RedisSection    Redis    { get; init; } = new();
	public required SecuritySection Security { get; init; } = new();
	public required StorageSection  Storage  { get; init; } = new();

	public sealed class InstanceSection {
		public readonly string Version;

		public InstanceSection() {
			// Get version information from assembly
			var version = Assembly.GetEntryAssembly()!
			                      .GetCustomAttributes().OfType<AssemblyInformationalVersionAttribute>()
			                      .First().InformationalVersion;

			// If we have a git revision, limit it to 10 characters
			if (version.Split('+') is { Length: 2 } split) {
				split[1] = split[1][..Math.Min(split[1].Length, 10)];
				Version  = string.Join('+', split);
			}
			else {
				Version = version;
			}
		}

		public string UserAgent => $"Iceshrimp.NET/{Version} (https://{WebDomain})";

		public int     ListenPort     { get; init; } = 3000;
		public string  ListenHost     { get; init; } = "localhost";
		public string? ListenSocket   { get; init; } = null;
		public string  WebDomain      { get; init; } = null!;
		public string  AccountDomain  { get; init; } = null!;
		public int     CharacterLimit { get; init; } = 8192;
	}

	public sealed class SecuritySection {
		public bool                 AuthorizedFetch      { get; init; } = true;
		public ExceptionVerbosity   ExceptionVerbosity   { get; init; } = ExceptionVerbosity.Basic;
		public Enums.Registrations  Registrations        { get; init; } = Enums.Registrations.Closed;
		public Enums.FederationMode FederationMode       { get; init; } = Enums.FederationMode.BlockList;
		public Enums.ItemVisibility ExposeFederationList { get; init; } = Enums.ItemVisibility.Registered;
		public Enums.ItemVisibility ExposeBlockReasons   { get; init; } = Enums.ItemVisibility.Registered;
	}

	public sealed class DatabaseSection {
		public string  Host     { get; init; } = "localhost";
		public int     Port     { get; init; } = 5432;
		public string  Database { get; init; } = null!;
		public string  Username { get; init; } = null!;
		public string? Password { get; init; }
	}

	public sealed class RedisSection {
		public string  Host     { get; init; } = "localhost";
		public int     Port     { get; init; } = 6379;
		public string? Prefix   { get; init; }
		public string? Username { get; init; }
		public string? Password { get; init; }
		public int?    Database { get; init; }

		//TODO: TLS settings
	}

	public sealed class StorageSection {
		private readonly TimeSpan?         _mediaRetention;
		public           Enums.FileStorage Mode { get; init; } = Enums.FileStorage.Local;

		public string? MediaRetention {
			get => _mediaRetention?.ToString();
			init {
				if (value == null || string.IsNullOrWhiteSpace(value) || value.Trim() == "0") {
					_mediaRetention = null;
					return;
				}

				if (value.Length < 2 || !int.TryParse(value[..^1].Trim(), out var num))
					throw new Exception("Invalid media retention time");

				var suffix = value[^1];

				_mediaRetention = suffix switch {
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

	public sealed class LocalStorageSection {
		public string? Path { get; init; }
	}

	public sealed class ObjectStorageSection {
		public string? Endpoint  { get; init; }
		public string? Region    { get; init; }
		public string? KeyId     { get; init; }
		public string? SecretKey { get; init; }
		public string? Bucket    { get; init; }
		public string? Prefix    { get; init; }
		public string? AccessUrl { get; init; }
	}
}