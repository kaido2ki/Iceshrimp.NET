using Iceshrimp.Backend.Core.Database;
using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace Iceshrimp.Backend.Controllers.Mastodon.Schemas.Entities;

public class ConversationEntity : IEntity
{
	[J("unread")]      public required bool                Unread     { get; set; }
	[J("accounts")]    public required List<AccountEntity> Accounts   { get; set; }
	[J("last_status")] public required StatusEntity        LastStatus { get; set; }
	[J("id")]          public required string              Id         { get; set; }
}