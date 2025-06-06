using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace Iceshrimp.Backend.Controllers.Pleroma.Schemas.Entities;

public class PleromaOauthTokenEntity
{
    [J("id")]          public required string   Id         { get; set; }
    [J("valid_until")] public required DateTime ValidUntil { get; set; }
    [J("app_name")]    public required string?  AppName    { get; set; }
}
