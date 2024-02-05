using System.Text.Json.Serialization;
using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;
using JI = System.Text.Json.Serialization.JsonIgnoreAttribute;

namespace Iceshrimp.Backend.Controllers.Mastodon.Schemas;

public class MastodonErrorResponse {
	[J("error")] public required string Error { get; set; }

	[J("error_description")]
	[JI(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public required string? Description { get; set; }
}