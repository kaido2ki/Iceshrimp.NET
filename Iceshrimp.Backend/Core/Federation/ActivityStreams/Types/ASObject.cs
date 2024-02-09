using Iceshrimp.Backend.Core.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using J = Newtonsoft.Json.JsonPropertyAttribute;
using JC = Newtonsoft.Json.JsonConverterAttribute;
using JR = Newtonsoft.Json.JsonRequiredAttribute;

namespace Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;

public class ASObject : ASIdObject {
	[J("@id")] [JR] public new required string Id { get; set; }

	[J("@type")]
	[JC(typeof(LDTypeConverter))]
	public string? Type { get; set; }

	public bool IsUnresolved => GetType() == typeof(ASObject) && Type == null;

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
				Types.Tombstone            => token.ToObject<ASTombstone>(),
				ASActivity.Types.Create    => token.ToObject<ASCreate>(),
				ASActivity.Types.Delete    => token.ToObject<ASDelete>(),
				ASActivity.Types.Follow    => token.ToObject<ASFollow>(),
				ASActivity.Types.Unfollow  => token.ToObject<ASUnfollow>(),
				ASActivity.Types.Accept    => token.ToObject<ASAccept>(),
				ASActivity.Types.Reject    => token.ToObject<ASReject>(),
				ASActivity.Types.Undo      => token.ToObject<ASUndo>(),
				ASActivity.Types.Like      => token.ToObject<ASActivity>(),
				_                          => token.ToObject<ASObject>()
			},
			JTokenType.Array => Deserialize(token.First()),
			JTokenType.String => new ASObject {
				Id = token.Value<string>() ??
				     throw new Exception("Encountered JTokenType.String with Value<string> null")
			},
			_ => throw new Exception($"Encountered JTokenType {token.Type}, which is not valid at this point")
		};
	}

	public static class Types {
		private const string Ns = Constants.ActivityStreamsNs;

		public const string Tombstone = $"{Ns}#Tombstone";
	}
}

public class ASTombstone : ASObject;

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