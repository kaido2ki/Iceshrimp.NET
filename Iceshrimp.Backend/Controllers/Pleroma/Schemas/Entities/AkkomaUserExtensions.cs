using Microsoft.EntityFrameworkCore;
using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace Iceshrimp.Backend.Controllers.Pleroma.Schemas.Entities;

[Keyless]
public class AkkomaUserExtensions
{
    [J("instance")]          public required AkkomaInstanceEntity Instance         { get; set; }
    [J("permit_followback")] public required bool?                PermitFollowback { get; set; }
}
