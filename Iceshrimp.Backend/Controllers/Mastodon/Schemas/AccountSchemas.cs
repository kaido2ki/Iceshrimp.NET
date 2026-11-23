using System.Text.Json;
using Iceshrimp.Backend.Controllers.Mastodon.Schemas.Entities;
using Microsoft.AspNetCore.Mvc;
using B = Microsoft.AspNetCore.Mvc.BindPropertyAttribute;
using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;
using JIgnore = System.Text.Json.Serialization.JsonIgnoreAttribute;
using JInclude = System.Text.Json.Serialization.JsonIncludeAttribute;

namespace Iceshrimp.Backend.Controllers.Mastodon.Schemas;

public abstract class AccountSchemas
{
	public class AccountStatusesRequest
	{
		[FromQuery(Name = "only_media")]      public bool    OnlyMedia      { get; set; } = false;
		[FromQuery(Name = "exclude_replies")] public bool    ExcludeReplies { get; set; } = false;
		[FromQuery(Name = "exclude_reblogs")] public bool    ExcludeRenotes { get; set; } = false;
		[FromQuery(Name = "exclude_direct")]  public bool    ExcludeDirect  { get; set; } = false;
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
		
		[J("attribution_domains")]
		[B(Name = "attribution_domains")]
		public List<string>? AttributionDomains { get; set; }

		// this is a little bit of a hack to allow serializing two different object types (hash / array) into the same variable
		// with .NET 11's union types this could likely be a lot cleaner
		[JInclude]
		[J("fields_attributes")]
		[B(Name = "fields_attributes")]
		private JsonElement? FieldsRaw { get; set; }

		[JIgnore]
		public IEnumerable<AccountUpdateField>? Fields
		{
			get
			{
				if (FieldsRaw is { ValueKind: JsonValueKind.Array })
				{
					return FieldsRaw.Value.Deserialize<IEnumerable<AccountUpdateField>>();
				}

				// Note that the integer index does not actually matter -- fields will be populated by the order in which they are provided.
				// https://github.com/mastodon/mastodon/issues/22174
				if (FieldsRaw is { ValueKind: JsonValueKind.Object })
				{
					return FieldsRaw.Value.Deserialize<OrderedDictionary<string, AccountUpdateField>>()?.Values;
				}

				return null;
			}
		}

		[J("permit_followback")]
		[B(Name = "permit_followback")]
		public bool? PermitFollowback { get; set; }

		[J("bite_controls")]
		[B(Name = "bite_controls")]
		public BiteControl? BiteControls { get; set; }

		[J("source")] [B(Name = "source")] public AccountUpdateSource? Source { get; set; }

		[B(Name = "avatar")] public IFormFile? Avatar { get; set; }
		[B(Name = "header")] public IFormFile? Banner { get; set; }

		[J("avatar_description")] [B(Name = "avatar_description")] public string? AvatarDescription { get; set; }
		[J("header_description")] [B(Name = "header_description")] public string? BannerDescription { get; set; }
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

	public class AccountMemoRequest
	{
		[J("comment")]
		[B(Name = "comment")]
		public required string Comment { get; set; }
	}

	public class AccountFollowSettingsRequest
	{
		[J("reblogs")]
		[B(Name = "reblogs")]
		public bool? Reblogs { get; set; }
	}
}