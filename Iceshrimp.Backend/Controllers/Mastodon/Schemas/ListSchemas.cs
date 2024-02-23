using B = Microsoft.AspNetCore.Mvc.BindPropertyAttribute;
using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;
using JR = System.Text.Json.Serialization.JsonRequiredAttribute;

namespace Iceshrimp.Backend.Controllers.Mastodon.Schemas;

public abstract class ListSchemas
{
	public class ListCreationRequest
	{
		[J("title")] [JR] [B(Name = "title")] public required string Title { get; set; }

		[J("replies_policy")]
		[B(Name = "replies_policy")]
		public string RepliesPolicy { get; set; } = "list";

		[J("exclusive")]
		[B(Name = "exclusive")]
		public bool Exclusive { get; set; } = false;
	}

	public class ListUpdateRequest
	{
		[J("title")] [B(Name = "title")] public string? Title { get; set; }

		[J("replies_policy")]
		[B(Name = "replies_policy")]
		public string? RepliesPolicy { get; set; }

		[J("exclusive")]
		[B(Name = "exclusive")]
		public bool? Exclusive { get; set; }
	}

	public class ListUpdateMembersRequest
	{
		[J("account_ids")]
		[JR]
		[B(Name = "account_ids")]
		public required List<string> AccountIds { get; set; }
	}
}