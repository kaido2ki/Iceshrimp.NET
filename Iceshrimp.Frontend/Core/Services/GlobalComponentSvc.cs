using Iceshrimp.Frontend.Components;

namespace Iceshrimp.Frontend.Core.Services;

public class GlobalComponentSvc
{
    public EmojiPicker?     EmojiPicker     { get; set; }
    public BannerContainer? BannerComponent { get; set; }
    public ConfirmDialog?   ConfirmDialog   { get; set; }
    public NoticeDialog?    NoticeDialog    { get; set; }
}
