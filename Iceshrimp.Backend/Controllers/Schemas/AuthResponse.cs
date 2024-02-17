using System.Text.Json;
using System.Text.Json.Serialization;
using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;
using JE = System.Runtime.Serialization.EnumMemberAttribute;

namespace Iceshrimp.Backend.Controllers.Schemas;

public class AuthStatusConverter() : JsonStringEnumConverter<AuthStatusEnum>(JsonNamingPolicy.SnakeCaseLower);

[JsonConverter(typeof(AuthStatusConverter))]
public enum AuthStatusEnum
{
	[JE(Value = "guest")]         Guest,
	[JE(Value = "authenticated")] Authenticated,
	[JE(Value = "2fa")]           TwoFactor
}

public class AuthResponse
{
	[J("status")] public required AuthStatusEnum Status { get; set; }
	[J("user")]   public          UserResponse?  User   { get; set; }
	[J("token")]  public          string?        Token  { get; set; }
}