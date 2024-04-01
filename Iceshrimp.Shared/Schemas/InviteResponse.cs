using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace Iceshrimp.Shared.Schemas;

public class InviteResponse
{
	[J("code")] public required string Code { get; set; }
}