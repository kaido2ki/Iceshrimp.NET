using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Extensions;
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
	public override bool CanWrite => true;

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
		writer.WriteStartObject();

		switch (value)
		{
			case DateTime dt:
				writer.WritePropertyName("@type");
				writer.WriteValue($"{Constants.XsdNs}#dateTime");
				writer.WritePropertyName("@value");
				writer.WriteValue(dt.ToStringIso8601Like());
				break;
			case uint ui:
				writer.WritePropertyName("@type");
				writer.WriteValue($"{Constants.XsdNs}#nonNegativeInteger");
				writer.WritePropertyName("@value");
				writer.WriteValue(ui);
				break;
			default:
				writer.WritePropertyName("@value");
				writer.WriteValue(value);
				break;
		}

		writer.WriteEndObject();
	}
}