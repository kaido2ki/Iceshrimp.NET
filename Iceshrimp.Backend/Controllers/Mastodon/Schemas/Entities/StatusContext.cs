using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace Iceshrimp.Backend.Controllers.Mastodon.Schemas.Entities;

public class StatusContext {
	[J("ancestors")]   public required List<Status> Ancestors   { get; set; }
	[J("descendants")] public required List<Status> Descendants { get; set; }
}