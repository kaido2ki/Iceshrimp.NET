using System.Reflection;

namespace Iceshrimp.Shared.Helpers;

public record VersionInfo(string Version, string RawVersion, string Codename, string Edition, string? CommitHash);

public static class VersionHelpers
{
	// Leave this as-is, unless you've forked the project. Set to a shorthand string that identifies your fork.
	// Gets appended to the version string like this: v1234.5+fork.commit
	private const string VersionIdentifier = "upstream";

	public static readonly Lazy<VersionInfo> VersionInfo = new(GetVersionInfo);

	private static VersionInfo GetVersionInfo()
	{
		var attributes = Assembly.GetExecutingAssembly()
		                         .GetCustomAttributes()
		                         .ToList();

		// Get codename from assembly
		var codename = attributes
		               .OfType<AssemblyMetadataAttribute>()
		               .FirstOrDefault(p => p.Key == "codename")
		               ?.Value ??
		               "unknown";

		// Get edition from assembly
		var edition = attributes
		              .OfType<AssemblyMetadataAttribute>()
		              .FirstOrDefault(p => p.Key == "edition")
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
			var maxLength = 0;

			// These suppressions are necessary because this is a compile-time constant that gets updated by forks
			#pragma warning disable CS8519 // The given expression never matches the provided pattern.
			#pragma warning disable CS8793 // The input always matches the provided pattern.
			#pragma warning disable CS8794 // The given expression always matches the provided pattern.
			// ReSharper disable HeuristicUnreachableCode
			if (VersionIdentifier is not "upstream")
			{
				split[1]  = $"{VersionIdentifier}.{split[1]}";
				maxLength = VersionIdentifier.Length + 1;
			}
			// ReSharper restore HeuristicUnreachableCode
			#pragma warning restore CS8519 // The given expression never matches the provided pattern.
			#pragma warning restore CS8794 // The input always matches the provided pattern.
			#pragma warning restore CS8793 // The given expression always matches the provided pattern.

			maxLength  += Math.Min(split[1].Length, 10);
			commitHash =  split[1];
			split[1]   =  split[1][..maxLength];
			version    =  string.Join('+', split);
			rawVersion =  split[0];
		}
		else
		{
			version    = fullVersion;
			rawVersion = fullVersion;
		}

		return new VersionInfo(version, rawVersion, codename, edition, commitHash);
	}
}