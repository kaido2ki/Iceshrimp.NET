using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Helpers;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Core.Services;

public class BiteService(DatabaseContext db, ActivityPub.ActivityRenderer activityRenderer, ActivityPub.ActivityDeliverService deliverSvc, NotificationService notificationSvc, IOptions<Config.InstanceSection> config)
{
    public async Task BiteAsync(User user, Bite target)
    {
        var bite = new Bite
        {
            Id         = IdHelpers.GenerateSnowflakeId(),
            CreatedAt  = DateTime.UtcNow,
            User       = user,
            TargetBite = target
        };
        bite.Uri = bite.GetPublicUri(config.Value);

        await db.Bites.AddAsync(bite);
        await db.SaveChangesAsync();

        if (target.UserHost != null)
        {
            var activity = activityRenderer.RenderBite(bite, target.Uri ?? target.GetPublicUri(config.Value), target.User);
            await deliverSvc.DeliverToAsync(activity, user, target.User);
        }

        await notificationSvc.GenerateBiteNotification(bite);
    }
    
    public async Task BiteAsync(User user, Note target)
    {
        var bite = new Bite
        {
            Id         = IdHelpers.GenerateSnowflakeId(),
            CreatedAt  = DateTime.UtcNow,
            User       = user,
            TargetNote = target
        };
        bite.Uri = bite.GetPublicUri(config.Value);

        await db.Bites.AddAsync(bite);
        await db.SaveChangesAsync();

        if (target.UserHost != null)
        {
            var activity = activityRenderer.RenderBite(bite, target.Uri ?? target.GetPublicUri(config.Value), target.User);
            await deliverSvc.DeliverToAsync(activity, user, target.User);
        }

        await notificationSvc.GenerateBiteNotification(bite);
    }
    
    public async Task BiteAsync(User user, User target)
    {
        var bite = new Bite
        {
            Id         = IdHelpers.GenerateSnowflakeId(),
            CreatedAt  = DateTime.UtcNow,
            User       = user,
            TargetUser = target
        };
        bite.Uri = bite.GetPublicUri(config.Value);

        await db.Bites.AddAsync(bite);
        await db.SaveChangesAsync();

        if (target.Host != null)
        {
            var activity = activityRenderer.RenderBite(bite, target.Uri ?? target.GetPublicUri(config.Value), target);
            await deliverSvc.DeliverToAsync(activity, user, target);
        }

        await notificationSvc.GenerateBiteNotification(bite);
    }
}