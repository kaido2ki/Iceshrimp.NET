using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace Iceshrimp.Backend.Controllers.Pleroma.Schemas.Entities;

public class PleromaNotificationExtensions
{
	[J("is_seen")] public required bool IsSeen { get; set; }
}