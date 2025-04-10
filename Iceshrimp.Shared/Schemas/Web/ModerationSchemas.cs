namespace Iceshrimp.Shared.Schemas.Web;

public class ModerationSchemas
{
	public class EmojiRefetchResponse
	{
		public required bool          Success { get; set; }
		public required EmojiResponse Emoji   { get; set; }
	}
}
