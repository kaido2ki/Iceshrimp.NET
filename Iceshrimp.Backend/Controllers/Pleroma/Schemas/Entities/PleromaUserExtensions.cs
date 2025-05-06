using Microsoft.EntityFrameworkCore;
using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace Iceshrimp.Backend.Controllers.Pleroma.Schemas.Entities;

[Keyless]
public class PleromaUserExtensions
{
    [J("favicon")]      public required string Favicon     { get; set; }
}
