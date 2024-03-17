using Iceshrimp.Backend.Controllers.Schemas;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Controllers.Renderers;

public class UserProfileRenderer(DatabaseContext db)
{
	public async Task<UserProfileResponse> RenderOne(User user, User? localUser, UserRendererDto? data = null)
	{
		var isFollowing  = (data?.Following ?? await GetFollowing([user], localUser)).Contains(user.Id);
		var ffVisibility = user.UserProfile?.FFVisibility ?? UserProfile.UserProfileFFVisibility.Public;
		var followers = ffVisibility switch
		{
			UserProfile.UserProfileFFVisibility.Public    => user.FollowersCount,
			UserProfile.UserProfileFFVisibility.Followers => isFollowing ? user.FollowersCount : null,
			UserProfile.UserProfileFFVisibility.Private   => (int?)null,
			_                                             => throw new ArgumentOutOfRangeException()
		};

		var following = ffVisibility switch
		{
			UserProfile.UserProfileFFVisibility.Public    => user.FollowingCount,
			UserProfile.UserProfileFFVisibility.Followers => isFollowing ? user.FollowingCount : null,
			UserProfile.UserProfileFFVisibility.Private   => (int?)null,
			_                                             => throw new ArgumentOutOfRangeException()
		};

		return new UserProfileResponse
		{
			Id        = user.Id,
			Bio       = user.UserProfile?.Description,
			Birthday  = user.UserProfile?.Birthday,
			Fields    = user.UserProfile?.Fields,
			Location  = user.UserProfile?.Location,
			Followers = followers,
			Following = following
		};
	}

	private async Task<List<string>> GetFollowing(IEnumerable<User> users, User? localUser)
	{
		if (localUser == null) return [];
		return await db.Followings.Where(p => p.Follower == localUser && users.Contains(p.Followee))
		               .Select(p => p.FolloweeId)
		               .ToListAsync();
	}

	public async Task<IEnumerable<UserProfileResponse>> RenderMany(IEnumerable<User> users, User? localUser)
	{
		var userList = users.ToList();
		var data     = new UserRendererDto { Following = await GetFollowing(userList, localUser) };
		return await userList.Select(p => RenderOne(p, localUser, data)).AwaitAllAsync();
	}

	public class UserRendererDto
	{
		public List<string>? Following;
	}
}