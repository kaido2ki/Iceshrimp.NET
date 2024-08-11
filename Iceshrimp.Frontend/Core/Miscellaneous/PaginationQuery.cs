namespace Iceshrimp.Frontend.Core.Miscellaneous;

internal class PaginationQuery
{
	public string? MaxId { get; init; }
	public string? MinId { get; init; }
	public int?    Limit { get; init; }

	public static implicit operator QueryString(PaginationQuery q) => q.ToQuery();

	private QueryString ToQuery()
	{
		var query = new QueryString();

		if (MaxId != null)
			query = query.Add("max_id", MaxId);
		if (MinId != null)
			query = query.Add("min_id", MinId);
		if (Limit.HasValue)
			query = query.Add("limit", Limit.Value.ToString());

		return query;
	}
}

internal class LinkPagination(int defaultLimit, int maxLimit) : Attribute
{
	public int DefaultLimit => defaultLimit;
	public int MaxLimit     => maxLimit;
}