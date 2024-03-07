using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace Iceshrimp.Backend.Controllers.Schemas;

public class ValueResponse(long count)
{
	[J("value")] public long Value { get; set; } = count;
}