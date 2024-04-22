namespace Iceshrimp.Shared.Schemas;

public class UserProfileResponse
{
	public required string                  Id        { get; set; }
	public required string?                 Birthday  { get; set; }
	public required string?                 Location  { get; set; }
	public required List<UserProfileField>? Fields    { get; set; }
	public required string?                 Bio       { get; set; }
	public required int?                    Followers { get; set; }
	public required int?                    Following { get; set; }
}

public class UserProfileField
{
	public required string Name     { get; set; }
	public required string Value    { get; set; }
	public          bool?  Verified { get; set; }
}