using J = Newtonsoft.Json.JsonPropertyAttribute;

namespace Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;

public class ASOrderedCollection<T> : ASCollection<T> where T : ASObject {
	[J("https://www.w3.org/ns/activitystreams#orderedItems")]
	public List<T>? OrderedItems { get; set; }
}