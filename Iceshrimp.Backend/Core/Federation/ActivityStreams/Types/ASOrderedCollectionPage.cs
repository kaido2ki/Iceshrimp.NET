using System.Diagnostics.CodeAnalysis;
using Iceshrimp.Backend.Core.Configuration;
using J = Newtonsoft.Json.JsonPropertyAttribute;
using JC = Newtonsoft.Json.JsonConverterAttribute;
using VC = Iceshrimp.Backend.Core.Federation.ActivityStreams.Types.ValueObjectConverter;

namespace Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;

public class ASOrderedCollectionPage : ASObject
{
	public ASOrderedCollectionPage() => Type = ObjectType;
	
	[SetsRequiredMembers]
	public ASOrderedCollectionPage(string id) : this() => Id = id;

	[J($"{Constants.ActivityStreamsNs}#items")]
	[JC(typeof(ASCollectionItemsConverter))]
	public List<ASObject>? Items { get; set; }

	[J($"{Constants.ActivityStreamsNs}#totalItems")]
	[JC(typeof(VC))]
	public ulong? TotalItems { get; set; }

	[J($"{Constants.ActivityStreamsNs}#partOf")]
	[JC(typeof(ASLinkConverter))]
	public ASLink? PartOf { get; set; }

	[J($"{Constants.ActivityStreamsNs}#prev")]
	[JC(typeof(ASLinkConverter))]
	public ASLink? Prev { get; set; }

	[J($"{Constants.ActivityStreamsNs}#next")]
	[JC(typeof(ASLinkConverter))]
	public ASLink? Next { get; set; }

	public const string ObjectType = $"{Constants.ActivityStreamsNs}#OrderedCollectionPage";
}

public sealed class ASOrderedCollectionPageConverter : ASSerializer.ListSingleObjectConverter<ASOrderedCollectionPage>;