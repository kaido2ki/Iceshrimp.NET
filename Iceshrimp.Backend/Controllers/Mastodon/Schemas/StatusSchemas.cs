using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;
using JR = System.Text.Json.Serialization.JsonRequiredAttribute;
using B = Microsoft.AspNetCore.Mvc.BindPropertyAttribute;

namespace Iceshrimp.Backend.Controllers.Mastodon.Schemas;

public abstract class StatusSchemas
{
	public class PostStatusRequest
	{
		[B(Name = "status")] [J("status")] public string? Text { get; set; }

		[B(Name = "in_reply_to_id")]
		[J("in_reply_to_id")]
		public string? ReplyId { get; set; }

		[B(Name = "sensitive")]
		[J("sensitive")]
		public bool Sensitive { get; set; } = false;

		[B(Name = "spoiler_text")]
		[J("spoiler_text")]
		public string? Cw { get; set; }

		[B(Name = "visibility")]
		[J("visibility")]
		[JR]
		public string Visibility { get; set; } = null!;

		[B(Name = "language")] [J("language")] public string? Language { get; set; }

		[B(Name = "scheduled_at")]
		[J("scheduled_at")]
		public string? ScheduledAt { get; set; }

		[B(Name = "media_ids")]
		[J("media_ids")]
		public List<string>? MediaIds { get; set; }

		[B(Name = "local_only")]
		[J("local_only")]
		public bool LocalOnly { get; set; } = false;

		[B(Name = "quote_id")] [J("quote_id")] public string? QuoteId { get; set; }

		[B(Name = "poll")] [J("poll")] public PollData? Poll { get; set; }

		public class PollData
		{
			[B(Name = "options")]
			[J("options")]
			[JR]
			public List<string> Options { get; set; } = null!;

			[B(Name = "expires_in")]
			[J("expires_in")]
			[JR]
			public long ExpiresIn { get; set; }

			[B(Name = "multiple")] [J("multiple")] public bool Multiple { get; set; } = false;

			[B(Name = "hide_totals")]
			[J("hide_totals")]
			public bool HideTotals { get; set; } = false;
		}
	}

	public class EditStatusRequest
	{
		[B(Name = "status")] [J("status")] public string? Text { get; set; }

		[B(Name = "sensitive")]
		[J("sensitive")]
		public bool Sensitive { get; set; } = false;

		[B(Name = "spoiler_text")]
		[J("spoiler_text")]
		public string? Cw { get; set; }

		[B(Name = "language")] [J("language")] public string? Language { get; set; }

		[B(Name = "media_ids")]
		[J("media_ids")]
		public List<string>? MediaIds { get; set; }

		[B(Name = "media_attributes")]
		[J("media_attributes")]
		public List<MediaAttributesEntry>? MediaAttributes { get; set; }

		[B(Name = "poll")] [J("poll")] public PostStatusRequest.PollData? Poll { get; set; }
	}

	public class MediaAttributesEntry : MediaSchemas.UpdateMediaRequest
	{
		[JR] [J("id")] [B(Name = "id")] public required string Id { get; set; }
	}

	public class ReblogRequest
	{
		[B(Name = "visibility")]
		[J("visibility")]
		public string? Visibility { get; set; }
	}
}