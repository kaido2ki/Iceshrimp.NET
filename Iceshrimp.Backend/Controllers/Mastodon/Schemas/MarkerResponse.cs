using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;
using B = Microsoft.AspNetCore.Mvc.BindPropertyAttribute;

namespace Iceshrimp.Backend.Controllers.Mastodon.Schemas;

public class MarkerSchemas
{
	public class MarkerPosition
	{
		[J("last_read_id")]
		[B(Name = "last_read_id")]
		public required string LastReadId { get; set; }
	}
}