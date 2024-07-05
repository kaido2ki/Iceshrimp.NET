namespace Iceshrimp.Shared.Schemas.Web;

public class FollowRequestResponse
{
	public required string       Id   { get; set; }
	public required UserResponse User { get; set; }
}