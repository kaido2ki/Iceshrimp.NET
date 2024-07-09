using System.Reflection;

namespace Iceshrimp.Shared.Helpers;

public record VersionInfo(string Version, string RawVersion, string Codename, string? CommitHash);

public static class VersionHelpers
{
	public static VersionInfo GetVersionInfo()
	{
		var attributes = Assembly.GetCallingAssembly()
		                         .GetCustomAttributes()
		                         .ToList();

		// Get codename from assembly
		var codename = attributes
		               .OfType<AssemblyMetadataAttribute>()
		               .FirstOrDefault(p => p.Key == "codename")
		               ?.Value ??
		               "unknown";

		string  version;
		string  rawVersion;
		string? commitHash = null;

		// Get version information from assembly
		var fullVersion = attributes.OfType<AssemblyInformationalVersionAttribute>()
		                            .First()
		                            .InformationalVersion;

		// If we have a git revision, limit it to 10 characters
		if (fullVersion.Split('+') is { Length: 2 } split)
		{
			commitHash = split[1];
			split[1]   = split[1][..Math.Min(split[1].Length, 10)];
			version    = string.Join('+', split);
			rawVersion = split[0];
		}
		else
		{
			version    = fullVersion;
			rawVersion = fullVersion;
		}

		return new VersionInfo(version, rawVersion, codename, commitHash);
	}
}