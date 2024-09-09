using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;
using B = Microsoft.AspNetCore.Mvc.BindPropertyAttribute;

namespace Iceshrimp.Backend.Controllers.Pleroma.Schemas;

public abstract class PleromaNotificationSchemas
{
	public class ReadNotificationsRequest
	{
		[B(Name = "id")]     [J("id")]     public long? Id    { get; set; }
		[B(Name = "max_id")] [J("max_id")] public long? MaxId { get; set; }
	}
}