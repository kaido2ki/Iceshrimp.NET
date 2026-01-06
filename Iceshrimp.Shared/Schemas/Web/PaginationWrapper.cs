namespace Iceshrimp.Shared.Schemas.Web;

/// <summary>
/// Data wrapped with pagination information
/// </summary>
/// <typeparam name="TData">Wrapped data</typeparam>
public class PaginationWrapper<TData>
{
    /// <summary>
    /// Pagination links
    /// </summary>
    public required PaginationData Links { get; set; }

    /// <summary>
    /// Wrapped data
    /// </summary>
    public required TData Data { get; set; }
}

/// <summary>
/// Pagination data
/// </summary>
public class PaginationData
{
    /// <summary>
    /// Number of items per page
    /// </summary>
    public required int Limit { get; set; }

    /// <summary>
    /// Link to the next page
    /// </summary>
    public string? Next { get; set; }

    /// <summary>
    /// Link to the previous page
    /// </summary>
    public string? Prev { get; set; }
}
