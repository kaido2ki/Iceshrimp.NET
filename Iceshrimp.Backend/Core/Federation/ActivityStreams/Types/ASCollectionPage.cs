using System.Diagnostics.CodeAnalysis;
using Iceshrimp.Backend.Core.Configuration;
using J = Newtonsoft.Json.JsonPropertyAttribute;
using JC = Newtonsoft.Json.JsonConverterAttribute;
using VC = Iceshrimp.Backend.Core.Federation.ActivityStreams.Types.ValueObjectConverter;

namespace Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;

public class ASCollectionPage : ASObject
{
	public ASCollectionPage() => Type = ObjectType;
	
	[SetsRequiredMembers]
	public ASCollectionPage(string id) : this() => Id = id;

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

	public const string ObjectType = $"{Constants.ActivityStreamsNs}#CollectionPage";
}

public sealed class ASCollectionPageConverter : ASSerializer.ListSingleObjectConverter<ASCollectionPage>;