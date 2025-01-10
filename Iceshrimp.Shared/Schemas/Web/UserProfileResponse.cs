using Iceshrimp.Shared.Helpers;

namespace Iceshrimp.Shared.Schemas.Web;

public class UserProfileResponse : IIdentifiable
{
	public required string                  Id        { get; set; }
	public required string?                 Birthday  { get; set; }
	public required string?                 Location  { get; set; }
	public required List<UserProfileField>? Fields    { get; set; }
	public required string?                 Bio       { get; set; }
	public required int?                    Followers { get; set; }
	public required int?                    Following { get; set; }
	public required Relations               Relations { get; set; }
	public required Role                    Role      { get; set; }
	public required bool                    IsLocked  { get; set; }
	public required string?                 Url       { get; set; }
}

[Flags]
public enum Relations
{
	None        = 0,
	Self        = 1,
	Following   = 2,
	FollowedBy  = 4,
	Requested   = 8,
	RequestedBy = 16,
	Blocking    = 32,
	Muting      = 64
}

public enum Role
{
	None      = 0,
	Moderator = 1,
	Admin     = 2
}

public class UserProfileField
{
	public required string Name     { get; set; }
	public required string Value    { get; set; }
	public          bool?  Verified { get; set; }
}