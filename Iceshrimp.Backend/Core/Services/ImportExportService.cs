using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using static Iceshrimp.Backend.Core.Federation.ActivityPub.UserResolver;

namespace Iceshrimp.Backend.Core.Services;

public class ImportExportService(
	DatabaseContext db,
	ILogger<UserService> logger,
	IOptions<Config.InstanceSection> instance,
	CacheService cacheSvc,
	UserService userSvc,
	ActivityPub.UserResolver userResolver
) : IScopedService
{
	public async Task<string> ExportBlockingAsync(User user)
	{
		var blockees = await db.Blockings
		                       .Where(p => p.BlockerId == user.Id)
		                       .Where(p => !p.Blockee.IsDeleted
		                                   && !p.Blockee.IsSystemUser
		                                   && p.Blockee.MovedToUri == null)
		                       .OrderBy(p => p.Blockee.Host)
		                       .ThenBy(p => p.Blockee.UsernameLower)
		                       .Select(p => p.Blockee.GetFqn(instance.Value.AccountDomain))
		                       .ToListAsync();

		return string.Join("\n", blockees);
	}

	public async Task<string> ExportFollowingAsync(User user)
	{
		var followees = await db.Followings
		                        .Where(p => p.FollowerId == user.Id)
		                        .Where(p => !p.Followee.IsDeleted
		                                    && !p.Followee.IsSystemUser
		                                    && p.Followee.MovedToUri == null)
		                        .OrderBy(p => p.Followee.Host)
		                        .ThenBy(p => p.Followee.UsernameLower)
		                        .Select(p => p.Followee.GetFqn(instance.Value.AccountDomain))
		                        .ToListAsync();

		return string.Join("\n", followees);
	}

	public async Task<string> ExportMutingAsync(User user)
	{
		var mutees = await db.Mutings
		                     .Where(p => p.MuterId == user.Id)
		                     .Where(p => !p.Mutee.IsDeleted && !p.Mutee.IsSystemUser && p.Mutee.MovedToUri == null)
		                     .OrderBy(p => p.Mutee.Host)
		                     .ThenBy(p => p.Mutee.UsernameLower)
		                     .Select(p => p.Mutee.GetFqn(instance.Value.AccountDomain))
		                     .ToListAsync();

		return string.Join("\n", mutees);
	}

	public async Task ImportBlockingAsync(User user, List<string> fqns)
	{
		foreach (var fqn in fqns)
		{
			try
			{
				var blockee = await userResolver.ResolveAsync($"acct:{fqn}", ResolveFlags.Acct);
				if (blockee.Id == user.Id) continue; // don't block self
				await userSvc.BlockUserAsync(user, blockee);
			}
			catch (Exception e)
			{
				logger.LogWarning("Failed to import block {blockee} for user {blocker}: {error}", fqn, user.Id, e);
			}
		}

		await QueryableTimelineExtensions.ResetHeuristicAsync(user, cacheSvc);
	}

	public async Task ImportFollowingAsync(User user, List<string> fqns)
	{
		foreach (var fqn in fqns)
		{
			try
			{
				var followee = await userResolver.ResolveAsync($"acct:{fqn}", ResolveFlags.Acct);
				if (followee.Id == user.Id) continue;
				await userSvc.FollowUserAsync(user, followee);
			}
			catch (Exception e)
			{
				logger.LogWarning("Failed to import follow {followee} for user {follower}: {error}",
				                  fqn, user.Id, e);
			}
		}

		await QueryableTimelineExtensions.ResetHeuristicAsync(user, cacheSvc);
	}

	public async Task ImportMutingAsync(User user, List<string> fqns)
	{
		foreach (var fqn in fqns)
		{
			try
			{
				var mutee = await userResolver.ResolveAsync($"acct:{fqn}", ResolveFlags.Acct);
				if (mutee.Id == user.Id) continue;
				await userSvc.MuteUserAsync(user, mutee, null);
			}
			catch (Exception e)
			{
				logger.LogWarning("Failed to import mute {mutee} for user {muter}: {error}", fqn, user.Id, e);
			}
		}

		await QueryableTimelineExtensions.ResetHeuristicAsync(user, cacheSvc);
	}
}