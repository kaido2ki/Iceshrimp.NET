using System.Reflection;

namespace Iceshrimp.Backend.Core.Configuration;

public sealed class Config {
	public required InstanceSection Instance { get; set; }
	public required DatabaseSection Database { get; set; }

	public sealed class InstanceSection {
		public readonly string Version;
		public          string UserAgent => $"Iceshrimp.NET/{Version} (https://{WebDomain})";

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

		public required int    ListenPort    { get; set; } = 3000;
		public required string WebDomain     { get; set; }
		public required string AccountDomain { get; set; }
	}

	public sealed class DatabaseSection {
		public required string  Host     { get; set; }
		public required string  Database { get; set; }
		public required string  Username { get; set; }
		public          string? Password { get; set; }
	}
}