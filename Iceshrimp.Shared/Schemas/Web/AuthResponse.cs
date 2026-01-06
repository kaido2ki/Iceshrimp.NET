namespace Iceshrimp.Shared.Schemas.Web;

public enum AuthStatusEnum
{
	Guest,
	Authenticated,
	TwoFactor
}

public class AuthResponse
{
	/// <summary>
	/// Authentication status. If this value is <c>two_factor</c> then two-factor authentication is required
	/// </summary>
	public required AuthStatusEnum Status      { get; set; }
	public          bool?          IsAdmin     { get; set; }
	public          bool?          IsModerator { get; set; }

	/// <summary>
	/// Bearer token for authentication
	/// </summary>
	public string? Token { get; set; }

	/// <summary>
	/// Authenticated user
	/// </summary>
	public UserResponse? User { get; set; }
}