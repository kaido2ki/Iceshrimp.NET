namespace Iceshrimp.Frontend.Core.Schemas;

// NOTE: All client preferences must have a sensible default value
public class ClientPreferences
{
    public ClientTheme             Theme         { get; set; } = ClientTheme.System;
    public string                  CustomCss     { get; set; } = "";
    public NotificationPreferences Notifications { get; set; } = new NotificationPreferences();
}

public enum ClientTheme
{
    System,
    Light,
    Dark,
}

public class NotificationPreferences
{
    public bool         Enabled     { get; set; }
    public List<string> FilterTypes { get; set; } = [];
}
