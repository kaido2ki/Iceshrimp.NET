using Iceshrimp.Shared.Helpers;

namespace Iceshrimp.Shared.Schemas.Web;

public class FollowRequestResponse : IIdentifiable
{
	public required string       Id   { get; set; }
	public required UserResponse User { get; set; }
}