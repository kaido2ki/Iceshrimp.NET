using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;
using JR = System.Text.Json.Serialization.JsonRequiredAttribute;

namespace Iceshrimp.Backend.Core.Federation.WebFinger;

public sealed class Link {
	[J("rel"), JR] public string  Rel  { get; set; } = null!;
	[J("type")]    public string? Type { get; set; } = null!;
	[J("href")]    public string? Href { get; set; }
}

public sealed class WebFingerResponse {
	[J("links"), JR]   public List<Link>   Links   { get; set; } = null!;
	[J("subject"), JR] public string       Subject { get; set; } = null!;
	[J("aliases")]     public List<string> Aliases { get; set; } = [];
}