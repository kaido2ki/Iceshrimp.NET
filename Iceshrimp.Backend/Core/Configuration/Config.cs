using System.Reflection;

namespace Iceshrimp.Backend.Core.Configuration;

public sealed class Config {
	public required InstanceSection Instance { get; init; }
	public required DatabaseSection Database { get; init; }
	public required SecuritySection Security { get; init; }

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
		public required bool AuthorizedFetch { get; set; }
	}

	public sealed class DatabaseSection {
		public required string  Host     { get; init; }
		public required string  Database { get; init; }
		public required string  Username { get; init; }
		public          string? Password { get; init; }
	}
}