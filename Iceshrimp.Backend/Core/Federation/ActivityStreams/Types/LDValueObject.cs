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
			if (list == null || list.Count == 0)
				return null;
			return HandleObject(list[0], objectType);
		}

		if (reader.TokenType == JsonToken.StartObject)
		{
			var obj      = JObject.Load(reader);
			var finalObj = obj.ToObject<LDValueObject<object?>>();
			return HandleObject(finalObj, objectType);
		}

		return null;
	}

	internal static object? HandleObject(LDValueObject<object?>? obj, Type objectType)
	{
		if (obj?.Value is string s && (objectType == typeof(DateTime?) || objectType == typeof(DateTime)))
		{
			var succeeded = DateTime.TryParse(s, out var result);
			return succeeded ? result : null;
		}

		if (objectType == typeof(uint?))
		{
			var val = obj?.Value;
			return val != null ? Convert.ToUInt32(val) : null;
		}

		if (objectType == typeof(ulong?))
		{
			var val = obj?.Value;
			return val != null ? Convert.ToUInt64(val) : null;
		}
		
		if (objectType == typeof(int?))
		{
			var val = obj?.Value;
			return val != null ? Convert.ToInt32(val) : null;
		}

		if (objectType == typeof(long?))
		{
			var val = obj?.Value;
			return val != null ? Convert.ToInt64(val) : null;
		}

		if (obj?.Value is string id)
		{
			if (objectType == typeof(ASOrderedCollection))
				return new ASOrderedCollection(id);
			if (objectType == typeof(ASCollection))
				return new ASCollection(id);
		}

		return obj?.Value;
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
			case ulong ul:
				writer.WritePropertyName("@type");
				writer.WriteValue($"{Constants.XsdNs}#nonNegativeInteger");
				writer.WritePropertyName("@value");
				writer.WriteValue(ul);
				break;
			case ASOrderedCollection oc:
				writer.WritePropertyName("@type");
				writer.WriteValue(ASOrderedCollection.ObjectType);
				writer.WritePropertyName("@value");
				writer.WriteRawValue(JsonConvert.SerializeObject(oc));
				break;
			case ASCollection c:
				writer.WritePropertyName("@type");
				writer.WriteValue(ASCollection.ObjectType);
				writer.WritePropertyName("@value");
				writer.WriteRawValue(JsonConvert.SerializeObject(c));
				break;
			default:
				writer.WritePropertyName("@value");
				writer.WriteValue(value);
				break;
		}

		writer.WriteEndObject();
	}
}