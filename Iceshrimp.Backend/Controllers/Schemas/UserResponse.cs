using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace Iceshrimp.Backend.Controllers.Schemas;

public class UserResponse
{
	[J("id")]              public required string  Id              { get; set; }
	[J("username")]        public required string  Username        { get; set; }
	[J("displayName")]     public required string? DisplayName     { get; set; }
	[J("avatarUrl")]       public required string? AvatarUrl       { get; set; }
	[J("bannerUrl")]       public required string? BannerUrl       { get; set; }
	[J("instanceName")]    public required string? InstanceName    { get; set; }
	[J("instanceIconUrl")] public required string? InstanceIconUrl { get; set; }
}