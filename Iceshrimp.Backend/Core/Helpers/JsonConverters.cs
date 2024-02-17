using System.Text.Json;
using System.Text.Json.Serialization;

namespace Iceshrimp.Backend.Core.Helpers;

public class EnsureArrayConverter : JsonConverter<List<string>>
{
	public override List<string> Read(
		ref Utf8JsonReader reader, Type typeToConvert,
		JsonSerializerOptions options
	)
	{
		if (reader.TokenType == JsonTokenType.StartArray)
		{
			var list = new List<string>();
			reader.Read();

			while (reader.TokenType != JsonTokenType.EndArray)
			{
				list.Add(JsonSerializer.Deserialize<string>(ref reader, options) ??
				         throw new InvalidOperationException());
				reader.Read();
			}

			return list;
		}

		if (reader.TokenType == JsonTokenType.String)
		{
			var str = JsonSerializer.Deserialize<string>(ref reader, options) ??
			          throw new InvalidOperationException();
			return [str];
		}

		throw new InvalidOperationException();
	}

	public override void Write(Utf8JsonWriter writer, List<string> value, JsonSerializerOptions options)
	{
		throw new NotImplementedException();
	}
}