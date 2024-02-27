using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;
using B = Microsoft.AspNetCore.Mvc.BindPropertyAttribute;

namespace Iceshrimp.Backend.Controllers.Mastodon.Schemas;

public class MarkerSchemas
{
	public class MarkerRequest
	{
		[J("home")]
		[B(Name = "home")]
		public MarkerPosition? Home { get; set; }
		
		[J("notifications")]
		[B(Name = "notifications")]
		public MarkerPosition? Notifications { get; set; }
	}

	public class MarkerPosition
	{
		[J("last_read_id")]
		[B(Name = "last_read_id")]
		public required string LastReadId { get; set; }
	}
}