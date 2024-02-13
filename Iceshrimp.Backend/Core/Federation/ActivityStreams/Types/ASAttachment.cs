using Iceshrimp.Backend.Core.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using J = Newtonsoft.Json.JsonPropertyAttribute;
using JC = Newtonsoft.Json.JsonConverterAttribute;

namespace Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;

public class ASAttachment : ASObjectBase {
	[J("@type")]
	[JC(typeof(StringListSingleConverter))]
	public string? Type { get; set; }
}

public class ASDocument : ASAttachment {
	[J("https://www.w3.org/ns/activitystreams#url")]
	[JC(typeof(ASObjectBaseConverter))]
	public ASObjectBase? Url { get; set; }

	[J("https://www.w3.org/ns/activitystreams#mediaType")]
	[JC(typeof(ValueObjectConverter))]
	public string? MediaType { get; set; }
	
	[J("https://www.w3.org/ns/activitystreams#sensitive")]
	[JC(typeof(ValueObjectConverter))]
	public bool? Sensitive { get; set; }
	
	[J("https://www.w3.org/ns/activitystreams#name")]
	[JC(typeof(ValueObjectConverter))]
	public string? Description { get; set; }
}

public sealed class ASAttachmentConverter : JsonConverter {
	public override bool CanWrite => false;

	public override bool CanConvert(Type objectType) {
		return true;
	}

	public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue,
	                                 JsonSerializer serializer) {
		if (reader.TokenType == JsonToken.StartObject) {
			var obj = JObject.Load(reader);
			return HandleObject(obj);
		}

		if (reader.TokenType == JsonToken.StartArray) {
			var array  = JArray.Load(reader);
			var result = new List<ASAttachment>();
			foreach (var token in array) {
				if (token is not JObject obj) return null;
				var item = HandleObject(obj);
				if (item == null) return null;
				result.Add(item);
			}

			return result;
		}

		return null;
	}

	private static ASAttachment? HandleObject(JToken obj) {
		var attachment = obj.ToObject<ASAttachment?>();

		return attachment?.Type == $"{Constants.ActivityStreamsNs}#Document"
			? obj.ToObject<ASDocument?>()
			: attachment;
	}

	public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer) {
		throw new NotImplementedException();
	}
}