using Iceshrimp.Shared.Helpers;
using static System.Text.Json.Serialization.JsonIgnoreCondition;
using JI = System.Text.Json.Serialization.JsonIgnoreAttribute;

namespace Iceshrimp.Shared.Schemas.Web;

public class UserResponse : IIdentifiable
{
	public required string  Id              { get; set; }
	public required string  Username        { get; set; }
	public required string? Host            { get; set; }
	public required string? DisplayName     { get; set; }
	public required string? AvatarUrl       { get; set; }
	public          string? AvatarAlt       { get; set; }
	public required string? BannerUrl       { get; set; }
	public          string? BannerAlt       { get; set; }
	public required string? InstanceName    { get; set; }
	public required string? InstanceIconUrl { get; set; }

	public bool                IsBot  { get; set; } = false;
	public bool                IsCat  { get; set; } = false;
	public List<EmojiResponse> Emojis { get; set; } = [];

	[JI(Condition = WhenWritingNull)] public string? MovedTo { get; set; }
}