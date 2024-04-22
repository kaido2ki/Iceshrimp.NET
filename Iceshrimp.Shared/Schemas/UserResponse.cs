namespace Iceshrimp.Shared.Schemas;

public class UserResponse
{
	public required string  Id              { get; set; }
	public required string  Username        { get; set; }
	public required string? DisplayName     { get; set; }
	public required string? AvatarUrl       { get; set; }
	public required string? BannerUrl       { get; set; }
	public required string? InstanceName    { get; set; }
	public required string? InstanceIconUrl { get; set; }
}