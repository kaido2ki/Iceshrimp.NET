using System.Diagnostics.CodeAnalysis;
using Iceshrimp.Backend.Core.Configuration;
using Newtonsoft.Json;
using J = Newtonsoft.Json.JsonPropertyAttribute;
using JC = Newtonsoft.Json.JsonConverterAttribute;

namespace Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;

public class ASOrderedCollectionPage : ASCollectionPage
{
	public new const string ObjectType = $"{Constants.ActivityStreamsNs}#OrderedCollectionPage";

	[JsonConstructor]
	public ASOrderedCollectionPage(bool withType = true) => Type = withType ? ObjectType : null;

	[SetsRequiredMembers]
	public ASOrderedCollectionPage(string id, bool withType = false) : this(withType) => Id = id;

	[J($"{Constants.ActivityStreamsNs}#items")]
	[JC(typeof(ASOrderedCollectionItemsConverter))]
	public new List<ASObject>? Items
	{
		get => base.Items;
		set => base.Items = value;
	}
}