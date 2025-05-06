using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace Iceshrimp.Backend.Controllers.Pleroma.Schemas.Entities;

public class AkkomaNodeInfoEntity
{
    [J("software")] public required AkkomaNodeInfoSoftwareEntity Software { get; set; }
}
