using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;
using JR = System.Text.Json.Serialization.JsonRequiredAttribute;
using B = Microsoft.AspNetCore.Mvc.BindPropertyAttribute;

namespace Iceshrimp.Backend.Controllers.Mastodon.Schemas;

public class PollSchemas
{
	public class PollVoteRequest
	{
		[B(Name = "choices")]
		[J("choices")]
		[JR]
		public required List<int> Choices { get; set; }
	}
}