using Iceshrimp.Backend.Core.Configuration;
using J = Newtonsoft.Json.JsonPropertyAttribute;
using JC = Newtonsoft.Json.JsonConverterAttribute;

namespace Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;

public class ASLink(string url) : ASObjectBase(url)
{
	[J($"{Constants.ActivityStreamsNs}#href")]
	[JC(typeof(ASObjectBaseConverter))]
	public ASObjectBase? Href { get; set; }

	[J($"{Constants.ActivityStreamsNs}#name")]
	[JC(typeof(ValueObjectConverter))]
	public string? Name { get; set; }

	public          string? Link       => Id ?? Href?.Id;
	public override string? ToString() => Link;
}

public class ASLinkConverter : ASSerializer.ListSingleObjectConverter<ASLink>;