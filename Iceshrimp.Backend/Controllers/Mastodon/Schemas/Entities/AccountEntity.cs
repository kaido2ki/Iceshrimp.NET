using Iceshrimp.Backend.Controllers.Pleroma.Schemas.Entities;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Shared.Helpers;
using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace Iceshrimp.Backend.Controllers.Mastodon.Schemas.Entities;

public class AccountEntity : IIdentifiable
{
	[J("username")]            public required string                 Username           { get; set; }
	[J("acct")]                public required string                 Acct               { get; set; }
	[J("fqn")]                 public required string                 FullyQualifiedName { get; set; }
	[J("display_name")]        public required string                 DisplayName        { get; set; }
	[J("locked")]              public required bool                   IsLocked           { get; set; }
	[J("created_at")]          public required string                 CreatedAt          { get; set; }
	[J("followers_count")]     public required long                   FollowersCount     { get; set; }
	[J("following_count")]     public required long                   FollowingCount     { get; set; }
	[J("statuses_count")]      public required long                   StatusesCount      { get; set; }
	[J("note")]                public required string                 Note               { get; set; }
	[J("url")]                 public required string                 Url                { get; set; }
	[J("uri")]                 public required string                 Uri                { get; set; }
	[J("avatar")]              public required string                 AvatarUrl          { get; set; }
	[J("avatar_static")]       public required string                 AvatarStaticUrl    { get; set; }
	[J("header")]              public required string                 HeaderUrl          { get; set; }
	[J("header_static")]       public required string                 HeaderStaticUrl    { get; set; }
	[J("moved")]               public required AccountEntity?         MovedToAccount     { get; set; }
	[J("bot")]                 public required bool                   IsBot              { get; set; }
	[J("discoverable")]        public required bool                   IsDiscoverable     { get; set; }
	[J("fields")]              public required List<Field>            Fields             { get; set; }
	[J("source")]              public          AccountSource?         Source             { get; set; }
	[J("emojis")]              public required List<EmojiEntity>      Emoji              { get; set; }
	[J("id")]                  public required string                 Id                 { get; set; }
	[J("last_status_at")]      public          string?                LastStatusAt       { get; set; }
	[J("attribution_domains")] public          List<string>?          AttributionDomains { get; set; }
	[J("pleroma")]             public required PleromaUserExtensions? Pleroma            { get; set; }
	[J("akkoma")]              public required AkkomaUserExtensions?  Akkoma             { get; set; }
	[J("bite_controls")]       public required BiteControl            BiteControls       { get; set; }
	[J("can_bite")]            public required bool                   CanBite            { get; set; }
	[J("role")]                public          UserRole?              Role               { get; set; }

	[J("avatar_description")] public required string AvatarDescription { get; set; }
	[J("header_description")] public required string HeaderDescription { get; set; }
	
	[J("formatted_note")]   public string?      FormattedNote   { get; set; }
	[J("formatted_fields")] public List<Field>? FormattedFields { get; set; }
}

public class Field
{
	[J("name")]        public required string  Name       { get; set; }
	[J("value")]       public required string  Value      { get; set; }
	[J("verified_at")] public          string? VerifiedAt { get; set; }
}

public class AccountSource
{
	[J("language")]              public required string       Language           { get; set; }
	[J("note")]                  public required string       Note               { get; set; }
	[J("privacy")]               public required string       Privacy            { get; set; }
	[J("sensitive")]             public required bool         Sensitive          { get; set; }
	[J("fields")]                public required List<Field>  Fields             { get; set; }
	[J("attribution_domains")]   public required List<string> AttributionDomains { get; set; }
	[J("follow_requests_count")] public required int          FollowRequestCount { get; set; }
}

public enum BiteControl
{
	Public,
	Followers,
	None
}

public class UserRole
{
	[J("id")]          public virtual string Id          { get; set; } = "user";
	[J("name")]        public virtual string Name        { get; set; } = "user";
	[J("color")]       public         string Color       { get; set; } = "";
	[J("highlighted")] public         bool   Highlighted { get; set; } = false;
	[J("permissions")] public virtual int    Permissions { get; set; } = 0;
}

public class ModeratorRole : UserRole
{
	[J("id")]          public override string Id          => "moderator";
	[J("name")]        public override string Name        => "moderator";
	[J("permissions")] public override int    Permissions => (int)Constants.MastodonRolePermissions.ModeratorRole;
}

public class AdminRole : UserRole
{
	[J("id")]          public override string Id          => "admin";
	[J("name")]        public override string Name        => "admin";
	[J("permissions")] public override int    Permissions => (int)Constants.MastodonRolePermissions.AdminRole;
}