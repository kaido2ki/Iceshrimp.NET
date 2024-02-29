using System.Diagnostics.CodeAnalysis;
using Iceshrimp.Backend.Core.Configuration;
using J = Newtonsoft.Json.JsonPropertyAttribute;

namespace Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;

public class ASOrderedCollection : ASCollection
{
	public ASOrderedCollection() => Type = ObjectType;
	
	[SetsRequiredMembers]
	public ASOrderedCollection(string id) : this() => Id = id;
    
	[J($"{Constants.ActivityStreamsNs}#orderedItems")]
	public List<ASObject>? OrderedItems { get; set; }
	
	public new const string ObjectType = $"{Constants.ActivityStreamsNs}#OrderedCollection";
}