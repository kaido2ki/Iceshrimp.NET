using System.Text.Json.Serialization;
using Iceshrimp.Backend.Core.Database;
using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;
using JI = System.Text.Json.Serialization.JsonIgnoreAttribute;

namespace Iceshrimp.Backend.Controllers.Mastodon.Schemas.Entities;

public class RelationshipEntity : IEntity {
	[J("following")]            public required bool   Following           { get; set; }
	[J("followed_by")]          public required bool   FollowedBy          { get; set; }
	[J("blocking")]             public required bool   Blocking            { get; set; }
	[J("blocked_by")]           public required bool   BlockedBy           { get; set; }
	[J("requested")]            public required bool   Requested           { get; set; }
	[J("requested_by")]         public required bool   RequestedBy         { get; set; }
	[J("muting")]               public required bool   Muting              { get; set; }
	[J("muting_notifications")] public required bool   MutingNotifications { get; set; }
	[J("domain_blocking")]      public required bool   DomainBlocking      { get; set; }
	[J("endorsed")]             public required bool   Endorsed            { get; set; }
	[J("showing_reblogs")]      public required bool   ShowingReblogs      { get; set; }
	[J("notifying")]            public required bool   Notifying           { get; set; }
	[J("note")]                 public required string Note                { get; set; }

	//TODO: implement this
	[J("languages")]
	[JI(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public List<string>? Languages { get; set; }

	[J("id")] public required string Id { get; set; }
}