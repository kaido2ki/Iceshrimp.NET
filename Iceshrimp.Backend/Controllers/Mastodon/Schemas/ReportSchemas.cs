using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;
using JR = System.Text.Json.Serialization.JsonRequiredAttribute;
using B = Microsoft.AspNetCore.Mvc.BindPropertyAttribute;

namespace Iceshrimp.Backend.Controllers.Mastodon.Schemas;

public abstract class ReportSchemas
{
	public class FileReportRequest
	{
		[B(Name = "account_id")]
		[J("account_id")]
		[JR]
		public string AccountId { get; set; } = null!;

		[B(Name = "status_ids")]
		[J("status_ids")]
		public string[] StatusIds { get; set; } = [];

		[B(Name = "rule_ids")]
		[J("rule_ids")]
		public string[] RuleIds { get; set; } = [];

		[B(Name = "comment")]
		[J("comment")]
		public string Comment { get; set; } = "";
	}
}
