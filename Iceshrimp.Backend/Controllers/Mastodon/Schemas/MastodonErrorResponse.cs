using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace Iceshrimp.Backend.Controllers.Mastodon.Schemas;

public class MastodonErrorResponse {
	[J("error")]             public required string  Error       { get; set; }
	[J("error_description")] public required string? Description { get; set; }
}