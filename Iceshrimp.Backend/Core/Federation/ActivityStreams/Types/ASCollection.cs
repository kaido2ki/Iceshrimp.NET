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
	public int? TotalItems { get; set; }
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
		if (obj.Count == 0) return null;

		var list = new List<ASObject>();
		foreach (var item in obj.SelectToken("..@list")!.Children())
			if (item.Type == JTokenType.String) {
				list.Add(new ASObject { Id = item.Value<string>() });
			}
			else if (item.Type == JTokenType.Object) {
				if (item["@type"]?[0]?.Type != JTokenType.String)
					throw new Exception("this shouldn't happen");

				ASObject? itemObj = item["@type"]?[0]?.Value<string>() switch {
					"https://www.w3.org/ns/activitystreams#Application"  => item.ToObject<ASActor>(),
					"https://www.w3.org/ns/activitystreams#Group"        => item.ToObject<ASActor>(),
					"https://www.w3.org/ns/activitystreams#Organization" => item.ToObject<ASActor>(),
					"https://www.w3.org/ns/activitystreams#Person"       => item.ToObject<ASActor>(),
					"https://www.w3.org/ns/activitystreams#Service"      => item.ToObject<ASActor>(),
					"https://www.w3.org/ns/activitystreams#Note"         => item.ToObject<ASNote>(),
					"https://www.w3.org/ns/activitystreams#Create"       => throw new ArgumentOutOfRangeException(),
					_                                                    => null
				};

				if (itemObj != null) list.Add(itemObj);
			}
			else {
				throw new Exception("blablabla");
			}

		return list;
	}

	public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer) {
		throw new NotImplementedException();
	}
}