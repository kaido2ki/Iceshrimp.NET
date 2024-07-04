namespace Iceshrimp.Shared.Schemas.Web;

public class AuthRequest
{
	public required string Username { get; set; }
	public required string Password { get; set; }
}

public class RegistrationRequest : AuthRequest
{
	public string? Invite { get; set; }
}

public class ChangePasswordRequest
{
	public required string OldPassword { get; set; }
	public required string NewPassword { get; set; }
}

public class ResetPasswordRequest
{
	public required string Password { get; set; }
}