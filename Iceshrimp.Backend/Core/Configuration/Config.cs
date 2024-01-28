using System.Reflection;
using Iceshrimp.Backend.Core.Middleware;

namespace Iceshrimp.Backend.Core.Configuration;

public sealed class Config {
	public required InstanceSection Instance { get; init; }
	public required DatabaseSection Database { get; init; }
	public required RedisSection    Redis    { get; init; }
	public required SecuritySection Security { get; init; } = new();

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

		public required int    ListenPort    { get; init; } = 3000;
		public required string WebDomain     { get; init; }
		public required string AccountDomain { get; init; }
	}

	public sealed class SecuritySection {
		public bool                AuthorizedFetch    { get; init; } = true;
		public ExceptionVerbosity  ExceptionVerbosity { get; init; } = ExceptionVerbosity.Basic;
		public Enums.Registrations Registrations      { get; init; } = Enums.Registrations.Closed;
	}

	public sealed class DatabaseSection {
		public required string  Host     { get; init; } = "localhost";
		public required int     Port     { get; init; } = 5432;
		public required string  Database { get; init; }
		public required string  Username { get; init; }
		public          string? Password { get; init; }
	}

	public sealed class RedisSection {
		public required string  Host     { get; init; } = "localhost";
		public required int     Port     { get; init; } = 6379;
		public          string? Prefix   { get; init; }
		public          string? Username { get; init; }
		public          string? Password { get; init; }
		public          int?    Database { get; init; }

		//TODO: TLS settings
	}
}