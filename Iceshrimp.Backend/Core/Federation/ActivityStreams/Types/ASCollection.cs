using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using J = Newtonsoft.Json.JsonPropertyAttribute;
using JC = Newtonsoft.Json.JsonConverterAttribute;
using VC = Iceshrimp.Backend.Core.Federation.ActivityStreams.Types.ValueObjectConverter;

namespace Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;

public class ASCollection<T>(string id) : LDIdObject(id) where T : ASObject {
	[J("https://www.w3.org/ns/activitystreams#items")]
	[JC(typeof(ASCollectionItemsConverter))]
	public List<T>? Items { get; set; }

	[J("https://www.w3.org/ns/activitystreams#totalItems")]
	[JC(typeof(VC))]
	public long? TotalItems { get; set; }
}

public sealed class ASCollection(string id) : ASCollection<ASObject>(id);

public sealed class ASCollectionConverter : ASSerializer.ListSingleObjectConverter<ASCollection>;

internal sealed class ASCollectionItemsConverter : JsonConverter {
	public override bool CanWrite => false;

	public override bool CanConvert(Type objectType) {
		return true;
	}

	public override object? ReadJson(JsonReader     reader, Type objectType, object? existingValue,
	                                 JsonSerializer serializer) {
		if (reader.TokenType != JsonToken.StartArray) throw new Exception("this shouldn't happen");

		var obj = JArray.Load(reader);
		return obj.Count == 0
			? null
			: obj.SelectToken("..@list")?.Children().Select(ASObject.Deserialize).OfType<ASObject>().ToList();
	}

	public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer) {
		throw new NotImplementedException();
	}
}