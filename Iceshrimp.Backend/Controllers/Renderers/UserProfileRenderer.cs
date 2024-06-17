using Iceshrimp.Shared.Schemas;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Controllers.Renderers;

public class UserProfileRenderer(DatabaseContext db)
{
	public async Task<UserProfileResponse> RenderOne(User user, User? localUser, UserRendererDto? data = null)
	{
		(data?.Relations ?? await GetRelations([user], localUser)).TryGetValue(user.Id, out var relations);
		relations ??= new RelationData
		{
			UserId        = user.Id,
			IsSelf        = user.Id == localUser?.Id,
			IsFollowing   = false,
			IsFollowedBy  = false,
			IsBlocking    = false,
			IsMuting      = false,
			IsRequested   = false,
			IsRequestedBy = false
		};

		var ffVisibility = user.UserProfile?.FFVisibility ?? UserProfile.UserProfileFFVisibility.Public;
		var followers = ffVisibility switch
		{
			UserProfile.UserProfileFFVisibility.Public    => user.FollowersCount,
			UserProfile.UserProfileFFVisibility.Followers => relations.IsFollowing ? user.FollowersCount : null,
			UserProfile.UserProfileFFVisibility.Private   => (int?)null,
			_                                             => throw new ArgumentOutOfRangeException()
		};

		var following = ffVisibility switch
		{
			UserProfile.UserProfileFFVisibility.Public    => user.FollowingCount,
			UserProfile.UserProfileFFVisibility.Followers => relations.IsFollowing ? user.FollowingCount : null,
			UserProfile.UserProfileFFVisibility.Private   => (int?)null,
			_                                             => throw new ArgumentOutOfRangeException()
		};

		var fields = user.UserProfile?.Fields.Select(p => new UserProfileField
		{
			Name     = p.Name,
			Value    = p.Value,
			Verified = p.IsVerified
		});

		return new UserProfileResponse
		{
			Id        = user.Id,
			Bio       = user.UserProfile?.Description,
			Birthday  = user.UserProfile?.Birthday,
			Fields    = fields?.ToList(),
			Location  = user.UserProfile?.Location,
			Followers = followers,
			Following = following,
			Relations = relations
		};
	}

	private async Task<Dictionary<string, RelationData>> GetRelations(IEnumerable<User> users, User? localUser)
	{
		var ids = users.Select(p => p.Id).ToList();
		if (ids.Count == 0) return [];
		if (localUser == null) return [];

		return await db.Users
		               .Where(p => ids.Contains(p.Id))
		               .Select(p => new RelationData
		               {
			               UserId        = p.Id,
			               IsSelf        = p.Id == localUser.Id,
			               IsFollowing   = p.IsFollowedBy(localUser),
			               IsFollowedBy  = p.IsFollowing(localUser),
			               IsBlocking    = p.IsBlockedBy(localUser),
			               IsMuting      = p.IsMutedBy(localUser),
			               IsRequested   = p.IsRequestedBy(localUser),
			               IsRequestedBy = p.IsRequested(localUser)
		               })
		               .ToDictionaryAsync(p => p.UserId, p => p);
	}

	public async Task<IEnumerable<UserProfileResponse>> RenderMany(IEnumerable<User> users, User? localUser)
	{
		var userList = users.ToList();
		var data     = new UserRendererDto { Relations = await GetRelations(userList, localUser) };
		return await userList.Select(p => RenderOne(p, localUser, data)).AwaitAllAsync();
	}

	public class RelationData
	{
		public required string UserId;
		public required bool   IsSelf;
		public required bool   IsFollowing;
		public required bool   IsFollowedBy;
		public required bool   IsRequested;
		public required bool   IsRequestedBy;
		public required bool   IsBlocking;
		public required bool   IsMuting;

		public static implicit operator Relations(RelationData data)
		{
			var res                     = Relations.None;
			if (data.IsSelf) res        |= Relations.Self;
			if (data.IsFollowing) res   |= Relations.Following;
			if (data.IsFollowedBy) res  |= Relations.FollowedBy;
			if (data.IsRequested) res   |= Relations.Requested;
			if (data.IsRequestedBy) res |= Relations.RequestedBy;
			if (data.IsBlocking) res    |= Relations.Blocking;
			if (data.IsMuting) res      |= Relations.Muting;
			return res;
		}
	}

	public class UserRendererDto
	{
		public Dictionary<string, RelationData>? Relations;
	}
}