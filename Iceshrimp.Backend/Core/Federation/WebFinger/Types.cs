using System.Diagnostics.CodeAnalysis;
using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;
using JR = System.Text.Json.Serialization.JsonRequiredAttribute;

namespace Iceshrimp.Backend.Core.Federation.WebFinger;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class WebFingerLink {
	[J("rel")] [JR] public string  Rel      { get; set; } = null!;
	[J("type")]     public string? Type     { get; set; }
	[J("href")]     public string? Href     { get; set; }
	[J("template")] public string? Template { get; set; }
}

[SuppressMessage("ReSharper", "CollectionNeverUpdated.Global")]
[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class WebFingerResponse {
	[J("links")] [JR]   public List<WebFingerLink> Links   { get; set; } = null!;
	[J("subject")] [JR] public string              Subject { get; set; } = null!;
	[J("aliases")]      public List<string>        Aliases { get; set; } = [];
}