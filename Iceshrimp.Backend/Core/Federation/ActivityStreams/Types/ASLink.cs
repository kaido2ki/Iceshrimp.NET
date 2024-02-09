using J = Newtonsoft.Json.JsonPropertyAttribute;
using JR = Newtonsoft.Json.JsonRequiredAttribute;

namespace Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;

public class ASLink(string url) : ASIdObject(url) {
	[J("https://www.w3.org/ns/activitystreams#href")]
	public string? Href { get; set; }

	public          string? Link       => Id ?? Href;
	public override string? ToString() => Link;
}

public class ASLinkConverter : ASSerializer.ListSingleObjectConverter<ASLink>;