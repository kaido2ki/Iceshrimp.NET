using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace Iceshrimp.Backend.Controllers.Pleroma.Schemas.Entities;

public class AkkomaInstanceEntity
{
    [J("name")]     public required string               Name     { get; set; }
    [J("nodeinfo")] public required AkkomaNodeInfoEntity NodeInfo { get; set; }
}
