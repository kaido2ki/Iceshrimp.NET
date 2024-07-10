using Iceshrimp.Shared.Helpers;

namespace Iceshrimp.Frontend.Core.Services;

public sealed class VersionService
{
	private readonly VersionInfo _versionInfo = VersionHelpers.GetVersionInfo();

	public string  Codename   => _versionInfo.Codename;
	public string  Edition    => _versionInfo.Edition;
	public string? CommitHash => _versionInfo.CommitHash;
	public string  RawVersion => _versionInfo.RawVersion;
	public string  Version    => _versionInfo.Version;
}