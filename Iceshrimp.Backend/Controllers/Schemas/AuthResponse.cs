using System.Text.Json.Serialization;
using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;
using JE = System.Runtime.Serialization.EnumMemberAttribute;

namespace Iceshrimp.Backend.Controllers.Schemas;

[JsonConverter(typeof(JsonStringEnumConverter<AuthStatusEnum>))]
public enum AuthStatusEnum {
	[JE(Value = "guest")]         Guest,
	[JE(Value = "authenticated")] Authenticated,
	[JE(Value = "2fa")]           TwoFactor
}

public class AuthResponse {
	[J("status")] public required AuthStatusEnum Status { get; set; }
	[J("token")]  public required string?        Token  { get; set; }
	[J("user")]   public required UserResponse?  User   { get; set; }
}