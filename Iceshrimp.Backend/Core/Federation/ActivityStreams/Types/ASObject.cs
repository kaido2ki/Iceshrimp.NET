using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using J = Newtonsoft.Json.JsonPropertyAttribute;
using JC = Newtonsoft.Json.JsonConverterAttribute;
using JR = Newtonsoft.Json.JsonRequiredAttribute;

namespace Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;

public class ASObject {
	[J("@id")] [JR] public required string Id { get; set; }

	[J("@type")]
	[JC(typeof(LDTypeConverter))]
	public string? Type { get; set; }

	//FIXME: don't recurse creates and co
	public static ASObject? Deserialize(JToken token) {
		return token.Type switch {
			JTokenType.Object => token["@type"]?[0]?.Value<string>() switch {
				ASActor.Types.Person       => token.ToObject<ASActor>(),
				ASActor.Types.Service      => token.ToObject<ASActor>(),
				ASActor.Types.Group        => token.ToObject<ASActor>(),
				ASActor.Types.Organization => token.ToObject<ASActor>(),
				ASActor.Types.Application  => token.ToObject<ASActor>(),
				ASNote.Types.Note          => token.ToObject<ASNote>(),
				ASActivity.Types.Create    => token.ToObject<ASActivity>(),
				ASActivity.Types.Delete    => token.ToObject<ASActivity>(),
				ASActivity.Types.Follow    => token.ToObject<ASActivity>(),
				ASActivity.Types.Unfollow  => token.ToObject<ASActivity>(),
				ASActivity.Types.Accept    => token.ToObject<ASActivity>(),
				ASActivity.Types.Undo      => token.ToObject<ASActivity>(),
				ASActivity.Types.Like      => token.ToObject<ASActivity>(),
				_                          => token.ToObject<ASObject>()
			},
			JTokenType.Array  => Deserialize(token.First()),
			JTokenType.String => new ASObject { Id = token.Value<string>() ?? "" },
			_                 => throw new ArgumentOutOfRangeException()
		};
	}
}

public sealed class LDTypeConverter : ASSerializer.ListSingleObjectConverter<string>;

internal sealed class ASObjectConverter : JsonConverter {
	public override bool CanWrite => false;

	public override bool CanConvert(Type objectType) {
		return true;
	}

	public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue,
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