using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace Iceshrimp.Backend.Controllers.Pleroma.Schemas.Entities;

public class PleromaNotificationExtensions 
{
	[J("is_muted")] public required bool IsMuted { get; set; }
	[J("is_seen")]  public required bool IsSeen  { get; set; }
}