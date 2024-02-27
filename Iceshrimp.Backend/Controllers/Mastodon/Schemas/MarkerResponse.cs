using System.Text.Json.Serialization;
using Iceshrimp.Backend.Controllers.Mastodon.Schemas.Entities;
using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;
using JI = System.Text.Json.Serialization.JsonIgnoreAttribute;
using B = Microsoft.AspNetCore.Mvc.BindPropertyAttribute;

namespace Iceshrimp.Backend.Controllers.Mastodon.Schemas;

public class MarkerSchemas
{
	public class MarkerResponse
	{
		[J("home")]
		[JI(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public MarkerEntity? Home { get; set; }

		[J("notifications")]
		[JI(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public MarkerEntity? Notifications { get; set; }
	}

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