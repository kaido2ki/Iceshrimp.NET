namespace Iceshrimp.Shared.Schemas.Web;

public class FilterResponse
{
	public required long                Id       { get; init; }
	public required string              Name     { get; init; }
	public required DateTime?           Expiry   { get; init; }
	public required List<string>        Keywords { get; init; }
	public required FilterAction        Action   { get; init; }
	public required List<FilterContext> Contexts { get; init; }

	public enum FilterContext
	{
		Home          = 0,
		Lists         = 1,
		Threads       = 2,
		Notifications = 3,
		Accounts      = 4,
		Public        = 5
	}

	public enum FilterAction
	{
		Warn = 0,
		Hide = 1
	}
}