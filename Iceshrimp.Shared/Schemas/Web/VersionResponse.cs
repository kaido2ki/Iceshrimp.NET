namespace Iceshrimp.Shared.Schemas.Web;

public class VersionResponse
{
	public required string  Codename   { get; set; }
	public required string  Edition    { get; set; }
	public required string? CommitHash { get; set; }
	public required string  RawVersion { get; set; }
	public required string  Version    { get; set; }
}