using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace Iceshrimp.Backend.Controllers.Mastodon.Schemas.Entities;

public class ReportEntity
{
	[J("id")]              public required string        Id            { get; set; }
	[J("action_taken")]    public required bool          ActionTaken   { get; set; }
	[J("action_taken_at")] public          string?       ActionTakenAt { get; set; }
	[J("category")]        public required string        Category      { get; set; }
	[J("comment")]         public required string        Comment       { get; set; }
	[J("forwarded")]       public required bool          Forwarded     { get; set; }
	[J("created_at")]      public required string        CreatedAt     { get; set; }
	[J("status_ids")]      public          string[]?     StatusIds     { get; set; }
	[J("rule_ids")]        public          string[]?     RuleIds       { get; set; }
	[J("target_account")]  public required AccountEntity TargetAccount { get; set; }
}
