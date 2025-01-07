using Iceshrimp.Shared.Helpers;

namespace Iceshrimp.Shared.Schemas.Web;

public class SessionSchemas
{
	public class SessionResponse : IIdentifiable
	{
		public required string    Id         { get; set; }
		public required bool      Current    { get; set; }
		public required bool      Active     { get; set; }
		public required DateTime  CreatedAt  { get; set; }
		public required DateTime? LastActive { get; set; }
	}

	public class MastodonSessionResponse : IIdentifiable
	{
		public required string               Id         { get; set; }
		public required bool                 Active     { get; set; }
		public required DateTime             CreatedAt  { get; set; }
		public required DateTime?            LastActive { get; set; }
		public required string               App        { get; set; }
		public required List<string>         Scopes     { get; set; }
		public required MastodonSessionFlags Flags      { get; set; }
	}

	public class MastodonSessionFlags
	{
		public required bool SupportsHtmlFormatting { get; set; }
		public required bool AutoDetectQuotes       { get; set; }
		public required bool IsPleroma              { get; set; }
		public required bool SupportsInlineMedia    { get; set; }
	}
}
