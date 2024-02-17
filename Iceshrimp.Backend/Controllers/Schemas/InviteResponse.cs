using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace Iceshrimp.Backend.Controllers.Schemas;

public class InviteResponse
{
	[J("code")] public required string Code { get; set; }
}