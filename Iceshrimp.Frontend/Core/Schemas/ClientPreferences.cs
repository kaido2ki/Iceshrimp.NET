namespace Iceshrimp.Frontend.Core.Schemas;

// NOTE: All client preferences must have a sensible default value
public class ClientPreferences
{
    public string                  CustomCss     { get; set; } = "";
    public NotificationPreferences Notifications { get; set; } = new NotificationPreferences();
    public WellbeingPreferences    Wellbeing     { get; set; } = new WellbeingPreferences();
    public string                  Language      { get; set; } = "followBrowser";
}

public class NotificationPreferences
{
    public bool         Enabled     { get; set; }
    public List<string> FilterTypes { get; set; } = [];
}

public class WellbeingPreferences
{
    public bool HideFollowCounts      { get; set; }
    public bool HideInteractionCounts { get; set; }
    public bool HideReactions         { get; set; }
    public bool HideQuotes            { get; set; }
    public bool HideRenotes           { get; set; }
}