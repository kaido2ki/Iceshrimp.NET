using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace Iceshrimp.Backend.Controllers.Pleroma.Schemas.Entities;

public class AkkomaNodeInfoSoftwareEntity
{
    [J("name")]    public required string? Name    { get; set; }
    [J("version")] public required string? Version { get; set; }
}
