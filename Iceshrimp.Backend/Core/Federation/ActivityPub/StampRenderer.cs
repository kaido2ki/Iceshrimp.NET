using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;
using Iceshrimp.Utils.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Core.Federation.ActivityPub;

public class StampRenderer(
    IOptions<Config.InstanceSection> config,
    UserRenderer userRenderer, 
    NoteRenderer noteRenderer) : IScopedService
{
    public string StampId(InteractionStamp stamp) => $"https://{config.Value.WebDomain}/stamp/{stamp.Id}";
    
    public ASQuoteAuthorization RenderStamp(InteractionStamp stamp) => new()
    {
        Id                = StampId(stamp),
        AttributedTo      = userRenderer.RenderLite(stamp.TargetNote.User),
        InteractingObject = noteRenderer.RenderLite(stamp.Note),
        InteractionTarget = noteRenderer.RenderLite(stamp.TargetNote),
    };
}
