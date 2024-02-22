using B = Microsoft.AspNetCore.Mvc.BindPropertyAttribute;

namespace Iceshrimp.Backend.Controllers.Mastodon.Schemas;

public abstract class ListSchemas
{
	public class ListCreationRequest
	{
		[B(Name = "title")]          public required string Title         { get; set; }
		[B(Name = "replies_policy")] public          string RepliesPolicy { get; set; } = "list";
		[B(Name = "exclusive")]      public          bool   Exclusive     { get; set; } = false;
	}

	public class ListUpdateMembersRequest
	{
		[B(Name = "account_ids")] public required List<string> AccountIds { get; set; }
	}
}