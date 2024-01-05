using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using J = Newtonsoft.Json.JsonPropertyAttribute;
using JC = Newtonsoft.Json.JsonConverterAttribute;
using VC = Iceshrimp.Backend.Core.Federation.ActivityStreams.Types.ValueObjectConverter;

namespace Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;

public class ASActivity : ASObject {
	[J("https://www.w3.org/ns/activitystreams#actor")]
	[JC(typeof(ASActorConverter))]
	public ASActor? Actor { get; set; }

	[J("https://www.w3.org/ns/activitystreams#object")]
	[JC(typeof(ASActivityObjectConverter))]
	public ASObject? Object { get; set; }
}

public sealed class ASActivityConverter : ASSerializer.ListSingleObjectConverter<ASActivity>;

internal sealed class ASActivityObjectConverter : JsonConverter {
	public override bool CanWrite => false;

	public override bool CanConvert(Type objectType) {
		return true;
	}

	public override object? ReadJson(JsonReader     reader, Type objectType, object? existingValue,
	                                 JsonSerializer serializer) {
		if (reader.TokenType == JsonToken.StartArray) {
			var obj = JArray.Load(reader);
			return ASObject.Deserialize(obj[0]);
		}

		if (reader.TokenType == JsonToken.StartObject) {
			var obj = JObject.Load(reader);
			return ASObject.Deserialize(obj);
		}
		
		throw new Exception("this shouldn't happen");
	}

	public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer) {
		throw new NotImplementedException();
	}
}