using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace Iceshrimp.Backend.Controllers.Pleroma.Schemas.Entities;

public class PleromaInstanceExtensions 
{
	[J("vapid_public_key")] public required string VapidPublicKey { get; set; }
}