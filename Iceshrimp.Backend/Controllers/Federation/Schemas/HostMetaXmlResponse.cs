using System.Diagnostics.CodeAnalysis;
using System.Xml.Serialization;

namespace Iceshrimp.Backend.Controllers.Federation.Schemas;

[XmlRoot("XRD", Namespace = "http://docs.oasis-open.org/ns/xri/xrd-1.0", IsNullable = false)]
public class HostMetaXmlResponse()
{
	[XmlElement("Link")] public required HostMetaXmlResponseLink Link;

	[SetsRequiredMembers]
	public HostMetaXmlResponse(string webDomain) : this() => Link = new HostMetaXmlResponseLink(webDomain);
}

public class HostMetaXmlResponseLink()
{
	[XmlAttribute("rel")]      public          string Rel = "lrdd";
	[XmlAttribute("template")] public required string Template;
	[XmlAttribute("type")]     public          string Type = "application/xrd+xml";

	[SetsRequiredMembers]
	public HostMetaXmlResponseLink(string webDomain) : this() =>
		Template = $"https://{webDomain}/.well-known/webfinger?resource={{uri}}";
}