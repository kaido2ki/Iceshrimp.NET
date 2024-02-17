using Iceshrimp.Backend.Core.Configuration;
using J = Newtonsoft.Json.JsonPropertyAttribute;

namespace Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;

public class ASOrderedCollection<T> : ASCollection<T> where T : ASObject
{
	[J($"{Constants.ActivityStreamsNs}#orderedItems")]
	public List<T>? OrderedItems { get; set; }
}