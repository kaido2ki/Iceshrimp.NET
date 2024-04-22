using System.Text.Json;
using System.Text.Json.Serialization;

namespace Iceshrimp.Shared.Configuration;

public static class JsonSerialization
{
	public static readonly JsonSerializerOptions Options = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		Converters           = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) }
	};
}