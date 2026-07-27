using Iceshrimp.Shared.Configuration;

namespace Iceshrimp.Shared.Schemas.Web;

public class InstanceResponse
{
    public required string        AccountDomain              { get; set; }
    public required string        WebDomain                  { get; set; }
    public required Registrations Registration               { get; set; } 
    public required string        VapidKey                   { get; set; }
    public required string        Name                       { get; set; }
    public required string?       IconUrl                    { get; set; }
    public required string?       BannerUrl                  { get; set; }
    public required string?       ThemeColor                 { get; set; }
    public required Limitations   Limits                     { get; set; }
    public required int           UserCount                  { get; set; }
    public required string?       Description                { get; set; }
    public required string?       ContactEmail               { get; set; }
    public required string        DefaultTranslationLanguage { get; set; }
}

public class StaffResponse
{
    public required List<UserResponse> Admins     { get; set; }
    public required List<UserResponse> Moderators { get; set; }
}

public enum Registrations
{
    Closed = 0,
    Invite = 1,
    Open   = 2
}

public class Limitations
{
    public required int NoteLength { get; set; }

    public int ProfileDescriptionLength => 2048;
    public int ProfileFieldsCount       => Limits.MaxProfileFields;
    public int ProfileFieldsNameLength  => Limits.MaxProfileFieldNameLength;
    public int ProfileFieldsValueLength => Limits.MaxProfileFieldValueLength;
}
