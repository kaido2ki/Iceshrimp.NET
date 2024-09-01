using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace Iceshrimp.Backend.Controllers.Pleroma.Schemas.Entities;

public class PleromaEmojiEntity
{
	[J("image_url")] public required string   ImageUrl { get; set; }
	[J("tags")]      public          string[] Tags     { get; set; } = [];
}