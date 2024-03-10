using System.Diagnostics.CodeAnalysis;
using System.Xml.Serialization;

namespace Iceshrimp.Backend.Controllers.Federation.Schemas;

[XmlRoot("XRD", Namespace = "http://docs.oasis-open.org/ns/xri/xrd-1.0", IsNullable = false)]
public class HostMetaXmlResponse()
{
	[SetsRequiredMembers]
	public HostMetaXmlResponse(string webDomain) : this() => Link = new HostMetaXmlResponseLink(webDomain);

	[XmlElement("Link")] public required HostMetaXmlResponseLink Link;
}

public class HostMetaXmlResponseLink()
{
	[SetsRequiredMembers]
	public HostMetaXmlResponseLink(string webDomain) : this() =>
		Template = $"https://{webDomain}/.well-known/webfinger?resource={{uri}}";

	[XmlAttribute("rel")]      public          string Rel  = "lrdd";
	[XmlAttribute("type")]     public          string Type = "application/xrd+xml";
	[XmlAttribute("template")] public required string Template;
}