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
		                       .Include(p => p.Blockee)
		                       .Where(p => p.BlockerId == user.Id)
		                       .Select(p => p.Blockee)
		                       .Where(p => !p.IsDeleted && !p.IsSystemUser && p.MovedToUri == null)
		                       .OrderBy(p => p.Host)
		                       .ThenBy(p => p.UsernameLower)
		                       .Select(p => p.GetFqn(instance.Value.AccountDomain))
		                       .ToListAsync();

		return string.Join("\n", blockees);
	}

	public async Task<string> ExportFollowingAsync(User user)
	{
		var followees = await db.Followings
		                        .Include(p => p.Followee)
		                        .Where(p => p.FollowerId == user.Id)
		                        .Select(p => p.Followee)
		                        .Where(p => !p.IsDeleted && !p.IsSystemUser && p.MovedToUri == null)
		                        .OrderBy(p => p.Host)
		                        .ThenBy(p => p.UsernameLower)
		                        .Select(p => p.GetFqn(instance.Value.AccountDomain))
		                        .ToListAsync();

		return string.Join("\n", followees);
	}

	public async Task<string> ExportMutingAsync(User user)
	{
		var mutees = await db.Mutings
		                     .Include(p => p.Mutee)
		                     .Where(p => p.MuterId == user.Id)
		                     .Select(p => p.Mutee)
		                     .Where(p => !p.IsDeleted && !p.IsSystemUser && p.MovedToUri == null)
		                     .OrderBy(p => p.Host)
		                     .ThenBy(p => p.UsernameLower)
		                     .Select(p => p.GetFqn(instance.Value.AccountDomain))
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