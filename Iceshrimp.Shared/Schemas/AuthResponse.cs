namespace Iceshrimp.Shared.Schemas;

public enum AuthStatusEnum
{
	Guest,
	Authenticated,
	TwoFactor
}

public class AuthResponse
{
	public required AuthStatusEnum Status      { get; set; }
	public          bool?          IsAdmin     { get; set; }
	public          bool?          IsModerator { get; set; }
	public          string?        Token       { get; set; }
	public          UserResponse?  User        { get; set; }
}