using static System.Text.Json.Serialization.JsonIgnoreCondition;
using JI = System.Text.Json.Serialization.JsonIgnoreAttribute;

namespace Iceshrimp.Shared.Schemas.Web;

public class UserResponse
{
	public required string  Id              { get; set; }
	public required string  Username        { get; set; }
	public required string? Host            { get; set; }
	public required string? DisplayName     { get; set; }
	public required string? AvatarUrl       { get; set; }
	public required string? BannerUrl       { get; set; }
	public required string? InstanceName    { get; set; }
	public required string? InstanceIconUrl { get; set; }

	public List<EmojiResponse> Emojis { get; set; } = [];

	[JI(Condition = WhenWritingNull)] public string? MovedTo { get; set; }
}