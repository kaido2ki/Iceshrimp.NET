using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using J = Newtonsoft.Json.JsonPropertyAttribute;

namespace Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;

public class LDValueObject<T>
{
	[J("@type")]  public          string? Type  { get; set; }
	[J("@value")] public required T       Value { get; set; }
}

public class ValueObjectConverter : JsonConverter
{
	public override bool CanWrite => false;

	public override bool CanConvert(Type objectType)
	{
		return true;
	}

	public override object? ReadJson(
		JsonReader reader, Type objectType, object? existingValue,
		JsonSerializer serializer
	)
	{
		if (reader.TokenType == JsonToken.StartArray)
		{
			var obj  = JArray.Load(reader);
			var list = obj.ToObject<List<LDValueObject<object?>>>();
			return list == null || list.Count == 0 ? null : list[0].Value;
		}

		if (reader.TokenType == JsonToken.StartObject)
		{
			var obj      = JObject.Load(reader);
			var finalObj = obj.ToObject<LDValueObject<object?>>();
			return finalObj?.Value;
		}

		return null;
	}

	public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
	{
		throw new NotImplementedException();
	}
}