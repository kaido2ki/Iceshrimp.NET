using System.Text;
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
    DriveService driveSvc,
    UserService userSvc,
    ActivityPub.UserResolver userResolver
)
{
    public async Task ExportFollowingAsync(User user)
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

        var stream = new MemoryStream(Encoding.UTF8.GetBytes(string.Join("\n", followees)));

        await driveSvc.StoreFile(stream, user,
                                 new DriveFileCreationRequest
                                 {
                                     Filename    = $"following-{DateTime.UtcNow:yyyy-MM-dd-HH-mm-ss}.csv",
                                     IsSensitive = false,
                                     MimeType    = "text/csv"
                                 }, true);
    }

    public async Task ImportFollowingAsync(User user, List<string> fqns)
    {
        foreach (var fqn in fqns)
        {
            var followee = await userResolver.ResolveAsync($"acct:{fqn}", ResolveFlags.Acct);

            try
            {
                await userSvc.FollowUserAsync(user, followee);
            }
            catch (Exception e)
            {
                logger.LogWarning("Failed to import follow {followee} for user {follower}: {error}",
                                  followee.Id, user.Id, e);
            }
        }

        await QueryableTimelineExtensions.ResetHeuristic(user, cacheSvc);
    }
}