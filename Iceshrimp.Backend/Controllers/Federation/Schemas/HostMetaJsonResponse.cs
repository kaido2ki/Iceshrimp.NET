using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace Iceshrimp.Backend.Controllers.Federation.Schemas;

public class HostMetaJsonResponse()
{
	public HostMetaJsonResponse(string webDomain) : this()
	{
		Links = [new HostMetaJsonResponseLink(webDomain)];
	}

	[J("links")] public List<HostMetaJsonResponseLink>? Links { get; set; }
}

public class HostMetaJsonResponseLink()
{
	public HostMetaJsonResponseLink(string webDomain) : this()
	{
		Rel      = "lrdd";
		Type     = "application/jrd+json";
		Template = $"https://{webDomain}/.well-known/webfinger?resource={{uri}}";
	}

	[J("rel")]      public string? Rel      { get; set; }
	[J("type")]     public string? Type     { get; set; }
	[J("template")] public string? Template { get; set; }
}