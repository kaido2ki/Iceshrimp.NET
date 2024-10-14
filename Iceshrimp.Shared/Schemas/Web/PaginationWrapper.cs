namespace Iceshrimp.Shared.Schemas.Web;

public class PaginationWrapper<TData>
{
	public required PaginationData Links { get; set; }
	public required TData          Data  { get; set; }
}

public class PaginationData
{
	public required int     Limit { get; set; }
	public          string? Next  { get; set; }
	public          string? Prev  { get; set; }
}