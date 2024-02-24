using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;
using JI = System.Text.Json.Serialization.JsonIgnoreAttribute;

namespace Iceshrimp.Backend.Controllers.Mastodon.Schemas.Entities;

public class EmojiEntity
{
	[JI]                     public required string  Id;
	[J("shortcode")]         public required string  Shortcode       { get; set; }
	[J("static_url")]        public required string  StaticUrl       { get; set; }
	[J("url")]               public required string  Url             { get; set; }
	[J("visible_in_picker")] public required bool    VisibleInPicker { get; set; }
	[J("category")]          public          string? Category        { get; set; }
}