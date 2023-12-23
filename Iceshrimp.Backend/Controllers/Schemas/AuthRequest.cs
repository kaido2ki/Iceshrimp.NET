using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace Iceshrimp.Backend.Controllers.Schemas;

public class AuthRequest {
	[J("username")] public required string Username { get; set; }
	[J("password")] public required string Password { get; set; }
}