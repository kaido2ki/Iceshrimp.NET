using Iceshrimp.Backend.Core.Database;
using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace Iceshrimp.Backend.Controllers.Mastodon.Schemas.Entities;

public class AccountEntity : IEntity
{
	[J("username")]        public required string            Username           { get; set; }
	[J("acct")]            public required string            Acct               { get; set; }
	[J("fqn")]             public required string            FullyQualifiedName { get; set; }
	[J("display_name")]    public required string            DisplayName        { get; set; }
	[J("locked")]          public required bool              IsLocked           { get; set; }
	[J("created_at")]      public required string            CreatedAt          { get; set; }
	[J("followers_count")] public required long              FollowersCount     { get; set; }
	[J("following_count")] public required long              FollowingCount     { get; set; }
	[J("statuses_count")]  public required long              StatusesCount      { get; set; }
	[J("note")]            public required string            Note               { get; set; }
	[J("url")]             public required string            Url                { get; set; }
	[J("avatar")]          public required string            AvatarUrl          { get; set; }
	[J("avatar_static")]   public required string            AvatarStaticUrl    { get; set; }
	[J("header")]          public required string            HeaderUrl          { get; set; }
	[J("header_static")]   public required string            HeaderStaticUrl    { get; set; }
	[J("moved")]           public required AccountEntity?    MovedToAccount     { get; set; }
	[J("bot")]             public required bool              IsBot              { get; set; }
	[J("discoverable")]    public required bool              IsDiscoverable     { get; set; }
	[J("fields")]          public required Field[]           Fields             { get; set; }
	[J("source")]          public          AccountSource?    Source             { get; set; }
	[J("emojis")]          public required List<EmojiEntity> Emoji              { get; set; }
	[J("id")]              public required string            Id                 { get; set; }
}

public class Field
{
	[J("name")]        public required string  Name       { get; set; }
	[J("value")]       public required string  Value      { get; set; }
	[J("verified_at")] public          string? VerifiedAt { get; set; }
}

public class AccountSource
{
	[J("language")]  public required string  Language  { get; set; }
	[J("note")]      public required string  Note      { get; set; }
	[J("privacy")]   public required string  Privacy   { get; set; }
	[J("sensitive")] public required bool    Sensitive { get; set; }
	[J("fields")]    public required Field[] Fields    { get; set; }
}