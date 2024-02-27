using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace Iceshrimp.Backend.Controllers.Mastodon.Schemas.Entities;

public class MarkerEntity
{
	[J("last_read_id")] public required string Position  { get; set; }
	[J("version")]      public required int    Version   { get; set; }
	[J("updated_at")]   public required string UpdatedAt { get; set; }
}