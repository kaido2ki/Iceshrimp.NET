using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace Iceshrimp.Backend.Controllers.Schemas;

public class AuthRequest
{
	[J("username")] public required string Username { get; set; }
	[J("password")] public required string Password { get; set; }
}

public class RegistrationRequest : AuthRequest
{
	[J("invite")] public string? Invite { get; set; }
}

public class ChangePasswordRequest
{
	[J("old_password")] public required string OldPassword { get; set; }
	[J("new_password")] public required string NewPassword { get; set; }
}