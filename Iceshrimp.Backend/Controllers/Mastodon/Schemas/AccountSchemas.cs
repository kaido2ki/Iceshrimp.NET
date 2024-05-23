using Microsoft.AspNetCore.Mvc;
using B = Microsoft.AspNetCore.Mvc.BindPropertyAttribute;
using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace Iceshrimp.Backend.Controllers.Mastodon.Schemas;

public abstract class AccountSchemas
{
	public class AccountStatusesRequest
	{
		[FromQuery(Name = "only_media")]      public bool    OnlyMedia      { get; set; } = false;
		[FromQuery(Name = "exclude_replies")] public bool    ExcludeReplies { get; set; } = false;
		[FromQuery(Name = "exclude_reblogs")] public bool    ExcludeRenotes { get; set; } = false;
		[FromQuery(Name = "pinned")]          public bool    Pinned         { get; set; } = false;
		[FromQuery(Name = "tagged")]          public string? Tagged         { get; set; }
	}

	public class AccountUpdateRequest
	{
		[J("display_name")]
		[B(Name = "display_name")]
		public string? DisplayName { get; set; }

		[J("note")] [B(Name = "note")]     public string? Bio      { get; set; }
		[J("locked")] [B(Name = "locked")] public bool?   IsLocked { get; set; }
		[J("bot")] [B(Name = "bot")]       public bool?   IsBot    { get; set; }

		[J("discoverable")]
		[B(Name = "discoverable")]
		public bool? IsExplorable { get; set; }

		[J("hide_collections")]
		[B(Name = "hide_collections")]
		public bool? HideCollections { get; set; }

		[J("indexable")]
		[B(Name = "indexable")]
		public bool? IsIndexable { get; set; }

		[J("fields_attributes")]
		[B(Name = "fields_attributes")]
		public List<AccountUpdateField>? Fields { get; set; }

		[J("source")] [B(Name = "source")] public AccountUpdateSource? Source { get; set; }

		[B(Name = "avatar")] public IFormFile? Avatar { get; set; }
		[B(Name = "header")] public IFormFile? Banner { get; set; }
	}

	public class AccountUpdateField
	{
		[J("name")] [B(Name = "name")]   public string? Name  { get; set; }
		[J("value")] [B(Name = "value")] public string? Value { get; set; }
	}

	public class AccountUpdateSource
	{
		[J("privacy")] [B(Name = "privacy")]   public string? Privacy  { get; set; }
		[J("language")] [B(Name = "language")] public string? Language { get; set; }

		[J("sensitive")]
		[B(Name = "sensitive")]
		public bool? Sensitive { get; set; }
	}

	public class AccountMuteRequest
	{
		[J("notifications")]
		[B(Name = "notifications")]
		public bool Notifications { get; set; } = true;

		[J("duration")] [B(Name = "duration")] public long Duration { get; set; } = 0;
	}
}