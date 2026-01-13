using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;
using Iceshrimp.Backend.Core.Helpers;
using Iceshrimp.Utils.DependencyInjection;

namespace Iceshrimp.Backend.Core.Services;

public class StampService(DatabaseContext db, ActivityPub.ActivityRenderer activityRenderer, ActivityPub.ActivityDeliverService activityDeliver) : IScopedService
{
    public async Task AcceptQuoteAsync(Note targetNote, Note quote, ASQuoteRequest request)
    {
        var stamp = new InteractionStamp
        {
            Id         = IdHelpers.GenerateSnowflakeId(),
            Type       = InteractionStamp.InteractionStampType.Quote,
            TargetNote = targetNote,
            Note       = quote
        };
        
        // before database save so if rendering fails we don't save the stamp
        var accept = activityRenderer.RenderAcceptStamp(stamp, request);

        db.Add(stamp);
        await db.SaveChangesAsync();
        
        await activityDeliver.DeliverToAsync(accept, targetNote.User, quote.User);
    }
}
