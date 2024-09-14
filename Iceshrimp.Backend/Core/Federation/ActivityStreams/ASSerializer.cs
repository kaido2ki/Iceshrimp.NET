using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Iceshrimp.Backend.Core.Federation.ActivityStreams;

public static class ASSerializer
{
	public class ListSingleObjectConverter<T> : JsonConverter
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
				var list = obj.ToObject<List<T?>>();
				return list == null || list.Count == 0 ? null : list[0];
			}

			if (reader.TokenType == JsonToken.StartObject)
			{
				var obj = JObject.Load(reader);
				return obj.ToObject<T?>();
			}

			if (reader.TokenType == JsonToken.String && typeof(T).IsAssignableFrom(reader.ValueType))
				return reader.Value;

			if (reader.TokenType == JsonToken.Integer && typeof(T).IsAssignableFrom(reader.ValueType))
				return reader.Value;

			if (reader.TokenType == JsonToken.Float && typeof(T).IsAssignableFrom(reader.ValueType))
				return reader.Value;

			return null;
		}

		public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
		{
			throw new NotImplementedException();
		}
	}
}