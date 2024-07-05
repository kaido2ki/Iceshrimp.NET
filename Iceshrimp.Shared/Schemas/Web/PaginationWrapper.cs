namespace Iceshrimp.Shared.Schemas.Web;

public class PaginationWrapper<T>
{
	public required string Id     { get; set; }
	public required T      Entity { get; set; }
}

public class FollowRequestResponse : PaginationWrapper<UserResponse>;