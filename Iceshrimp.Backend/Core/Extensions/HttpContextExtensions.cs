using Iceshrimp.Backend.Controllers.Shared.Attributes;
using Iceshrimp.Backend.Controllers.Shared.Schemas;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Shared.Schemas.Web;
using Microsoft.AspNetCore.Http.Extensions;

namespace Iceshrimp.Backend.Core.Extensions;

public static partial class HttpContextExtensions
{
	public static PaginationWrapper<TData> CreatePaginationWrapper<TData>(
		this HttpContext ctx, PaginationQuery query, IEnumerable<IEntity> paginationData, TData data
	)
	{
		var attr = ctx.GetEndpoint()?.Metadata.GetMetadata<RestPaginationAttribute>();
		if (attr == null) throw new Exception("Route doesn't have a RestPaginationAttribute");

		var limit = Math.Min(query.Limit ?? attr.DefaultLimit, attr.MaxLimit);
		if (limit < 1) throw GracefulException.BadRequest("Limit cannot be less than 1");

		var ids = paginationData.Select(p => p.Id).ToList();
		if (query.MinId != null) ids.Reverse();

		var next = ids.Count >= limit ? new QueryBuilder { { "max_id", ids.Last() } } : null;
		var prev = ids.Count > 0 ? new QueryBuilder { { "min_id", ids.First() } } : null;

		var links = new PaginationData
		{
			Limit = limit,
			Next  = next?.ToQueryString().ToString(),
			Prev  = prev?.ToQueryString().ToString()
		};

		return new PaginationWrapper<TData> { Data = data, Links = links };
	}

	public static PaginationWrapper<TData> CreatePaginationWrapper<TData>(
		this HttpContext ctx, PaginationQuery query, TData data
	) where TData : IEnumerable<IEntity>
	{
		return CreatePaginationWrapper(ctx, query, data, data);
	}
}

public class RestPaginationAttribute(int defaultLimit, int maxLimit) : Attribute, IPaginationAttribute
{
	public int DefaultLimit => defaultLimit;
	public int MaxLimit     => maxLimit;
}