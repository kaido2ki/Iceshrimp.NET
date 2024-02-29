using System.Diagnostics.CodeAnalysis;
using Iceshrimp.Backend.Core.Configuration;
using J = Newtonsoft.Json.JsonPropertyAttribute;
using JC = Newtonsoft.Json.JsonConverterAttribute;
using JI = Newtonsoft.Json.JsonIgnoreAttribute;

namespace Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;

public class ASOrderedCollection : ASCollection
{
	public ASOrderedCollection() => Type = ObjectType;

	[SetsRequiredMembers]
	public ASOrderedCollection(string id) : this() => Id = id;

	[J($"{Constants.ActivityStreamsNs}#orderedItems")]
	[JC(typeof(ASCollectionItemsConverter))]
	[JI]
	public List<ASObject>? OrderedItems
	{
		get => Items;
		set => Items = value;
	}

	public new const string ObjectType = $"{Constants.ActivityStreamsNs}#OrderedCollection";
}

public sealed class ASOrderedCollectionConverter : ASSerializer.ListSingleObjectConverter<ASOrderedCollection>;