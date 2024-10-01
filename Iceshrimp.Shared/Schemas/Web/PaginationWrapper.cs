namespace Iceshrimp.Shared.Schemas.Web;

public class PaginationWrapper<T>
{
	/// <summary>
	///     Previous page (scrolling up, newer items on reverse chronological sort)
	/// </summary>
	public required string? PageUp { get; set; }

	/// <summary>
	///     Next page (scrolling down, older items on reverse chronological sort)
	/// </summary>
	public required string? PageDown { get; set; }

	public required List<T> Items { get; set; }
}
