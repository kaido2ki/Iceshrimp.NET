using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Helpers;
using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;
using JR = System.Text.Json.Serialization.JsonRequiredAttribute;
using JC = System.Text.Json.Serialization.JsonConverterAttribute;
using B = Microsoft.AspNetCore.Mvc.BindPropertyAttribute;

namespace Iceshrimp.Backend.Controllers.Mastodon.Schemas;

public class Account {
	[J("id")]              public required string   Id                 { get; set; }
	[J("username")]        public required string   Username           { get; set; }
	[J("acct")]            public required string   Acct               { get; set; }
	[J("fqn")]             public required string   FullyQualifiedName { get; set; }
	[J("display_name")]    public required string   DisplayName        { get; set; }
	[J("locked")]          public required bool     IsLocked           { get; set; }
	[J("created_at")]      public required string   CreatedAt          { get; set; }
	[J("followers_count")] public required long     FollowersCount     { get; set; }
	[J("following_count")] public required long     FollowingCount     { get; set; }
	[J("statuses_count")]  public required long     StatusesCount      { get; set; }
	[J("note")]            public required string   Note               { get; set; }
	[J("url")]             public required string   Url                { get; set; }
	[J("avatar")]          public required string   AvatarUrl          { get; set; }
	[J("avatar_static")]   public required string   AvatarStaticUrl    { get; set; }
	[J("header")]          public required string   HeaderUrl          { get; set; }
	[J("header_static")]   public required string   HeaderStaticUrl    { get; set; }
	[J("moved")]           public required Account? MovedToAccount     { get; set; }
	[J("bot")]             public required bool     IsBot              { get; set; }
	[J("discoverable")]    public required bool     IsDiscoverable     { get; set; }

	[J("source")] public string?             Source => null; //FIXME
	[J("fields")] public IEnumerable<string> Fields => [];   //FIXME
	[J("emojis")] public IEnumerable<string> Emoji  => [];   //FIXME
}