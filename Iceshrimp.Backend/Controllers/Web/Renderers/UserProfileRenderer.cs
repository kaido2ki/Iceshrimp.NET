using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Shared.Schemas.Web;
using Iceshrimp.Utils.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Controllers.Web.Renderers;

public class UserProfileRenderer(DatabaseContext db, IOptions<Config.InstanceSection> instance) : IScopedService
{
	public async Task<UserProfileResponse> RenderOne(User user, User? localUser, UserRendererDto? data = null)
	{
		(data?.Relations ?? await GetRelationsAsync([user], localUser)).TryGetValue(user.Id, out var relations);
		relations ??= new RelationData
		{
			UserId        = user.Id,
			IsSelf        = user.Id == localUser?.Id,
			IsFollowing   = false,
			IsFollowedBy  = false,
			IsBlocking    = false,
			IsMuting      = false,
			IsRequested   = false,
			IsRequestedBy = false,
			CanBite       = false,
			MutedRenotes  = false
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

		var role = user.IsAdmin
			? Role.Admin
			: user.IsModerator
				? Role.Moderator
				: Role.None;

		var url = user.Host != null ? user.UserProfile?.Url ?? user.Uri : user.GetPublicUrl(instance.Value);

		(data?.Memos ?? await GetMemosAsync([user], localUser)).TryGetValue(user.Id, out var memo);

		return new UserProfileResponse
		{
			Id        = user.Id,
			Bio       = user.UserProfile?.Description,
			Birthday  = user.UserProfile?.Birthday,
			Fields    = fields?.ToList(),
			Location  = user.UserProfile?.Location,
			Followers = followers,
			Following = following,
			Relations = relations,
			Role      = role,
			IsLocked  = user.IsLocked,
			Url       = url,
			Lang      = user.UserProfile?.Lang,
			CreatedAt = user.CreatedAt,
			ActiveAt  = user.LastActiveDate,
			Memo      = memo,
			Pronouns  = user.UserProfile?.Pronouns
		};
	}

	private async Task<Dictionary<string, RelationData>> GetRelationsAsync(IEnumerable<User> users, User? localUser)
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
			               IsRequestedBy = p.IsRequested(localUser),
			               CanBite = p.Id != localUser.Id
			                         && (p.CanBite == User.BiteControl.Public
			                             || (p.CanBite == User.BiteControl.Followers
			                                 && p.IsFollowedBy(localUser))),
			               MutedRenotes  = p.MutedRenotes(localUser)
		               })
		               .ToDictionaryAsync(p => p.UserId, p => p);
	}

	private async Task<Dictionary<string, string>> GetMemosAsync(IEnumerable<User> users, User? localUser)
	{
		var ids = users.Select(p => p.Id).ToList();
		if (ids.Count == 0) return [];
		if (localUser == null) return [];

		return await db.UserMemos
		               .Where(p => p.ByUserId == localUser.Id && ids.Contains(p.TargetUserId))
		               .ToDictionaryAsync(p => p.TargetUserId, p => p.Text);
	}

	public async Task<IEnumerable<UserProfileResponse>> RenderManyAsync(IEnumerable<User> users, User? localUser)
	{
		var userList = users.ToList();
		var data = new UserRendererDto
		{
			Relations = await GetRelationsAsync(userList, localUser),
			Memos     = await GetMemosAsync(userList, localUser)
		};
		return await userList.Select(p => RenderOne(p, localUser, data)).AwaitAllAsync();
	}

	public class RelationData
	{
		public required bool   CanBite;
		public required bool   IsBlocking;
		public required bool   IsFollowedBy;
		public required bool   IsFollowing;
		public required bool   IsMuting;
		public required bool   IsRequested;
		public required bool   IsRequestedBy;
		public required bool   IsSelf;
		public required bool   MutedRenotes;
		public required string UserId;

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
			if (data.CanBite) res       |= Relations.CanBite;
			if (data.MutedRenotes) res  |= Relations.MutedRenotes;
			return res;
		}
	}

	public class UserRendererDto
	{
		public Dictionary<string, RelationData>? Relations;
		public Dictionary<string, string>?       Memos;
	}
}