using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace Iceshrimp.Backend.Controllers.Schemas;

public class UserResponse {
	[J("id")]        public required string  Id        { get; set; }
	[J("username")]  public required string  Username  { get; set; }
	[J("avatarUrl")] public          string? AvatarUrl { get; set; }
	[J("bannerUrl")] public          string? BannerUrl { get; set; }
}