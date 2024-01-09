using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;
using JR = System.Text.Json.Serialization.JsonRequiredAttribute;

namespace Iceshrimp.Backend.Core.Federation.WebFinger;

public sealed class Link {
	[J("href"), JR] public string  Href { get; set; } = null!;
	[J("rel")]      public string? Rel  { get; set; }
}

public sealed class WebFingerResponse {
	[J("links"), JR]   public List<Link> Links   { get; set; } = null!;
	[J("subject"), JR] public string     Subject { get; set; } = null!;
}