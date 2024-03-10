using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace Iceshrimp.Backend.Controllers.Federation.Schemas;

public class HostMetaJsonResponse(string webDomain)
{
	[J("links")] public List<HostMetaJsonResponseLink> Links => [new HostMetaJsonResponseLink(webDomain)];
}

public class HostMetaJsonResponseLink(string webDomain)
{
	[J("rel")]      public string Rel      => "lrdd";
	[J("type")]     public string Type     => "application/jrd+json";
	[J("template")] public string Template => $"https://{webDomain}/.well-known/webfinger?resource={{uri}}";
}