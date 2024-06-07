using Iceshrimp.Backend.Controllers.Mastodon.Schemas;
using Iceshrimp.Backend.Core.Database;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Iceshrimp.Backend.Controllers.Attributes;

public class LinkPaginationAttribute(int defaultLimit, int maxLimit, bool offset = false) : ActionFilterAttribute
{
	public int DefaultLimit => defaultLimit;
	public int MaxLimit     => maxLimit;

	public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
	{
		ArgumentNullException.ThrowIfNull(context, nameof(context));
		ArgumentNullException.ThrowIfNull(next, nameof(next));
		OnActionExecuting(context);
		if (context.Result != null) return;
		var result = await next();
		HandlePagination(context.ActionArguments, result);
		OnActionExecuted(result);
	}

	private void HandlePagination(IDictionary<string, object?> actionArguments, ActionExecutedContext context)
	{
		if (actionArguments.Count == 0) return;

		var query = actionArguments.Values.OfType<IPaginationQuery>().FirstOrDefault();
		if (query == null) return;

		if (context.Result is not OkObjectResult result) return;
		if ((context.HttpContext.GetPaginationData() ?? result.Value) is not IEnumerable<IEntity> entities) return;
		var ids = entities.Select(p => p.Id).ToList();
		if (ids.Count == 0) return;
		if (query.MinId != null) ids.Reverse();

		List<string> links = [];

		var limit   = Math.Min(query.Limit ?? defaultLimit, maxLimit);
		var request = context.HttpContext.Request;

		var mpq      = query as MastodonPaginationQuery;
		var offsetPg = offset || mpq is { Offset: not null, MaxId: null, MinId: null, SinceId: null };
		if (ids.Count >= limit)
		{
			var next = offsetPg
				? new QueryBuilder { { "offset", ((mpq?.Offset ?? 0) + limit).ToString() } }
				: new QueryBuilder { { "limit", limit.ToString() }, { "max_id", ids.Last() } };

			links.Add($"<{GetUrl(request, next.ToQueryString())}>; rel=\"next\"");
		}

		var prev = offsetPg
			? new QueryBuilder { { "offset", Math.Max(0, (mpq?.Offset ?? 0) - limit).ToString() } }
			: new QueryBuilder { { "limit", limit.ToString() }, { "min_id", ids.First() } };

		if (!offsetPg || (mpq?.Offset ?? 0) != 0)
			links.Add($"<{GetUrl(request, prev.ToQueryString())}>; rel=\"prev\"");

		context.HttpContext.Response.Headers.Link = string.Join(", ", links);
	}

	private static string GetUrl(HttpRequest request, QueryString query)
	{
		return UriHelper.BuildAbsolute("https", request.Host, request.PathBase, request.Path, query);
	}
}

public static class HttpContextExtensions
{
	private const string Key = "link-pagination";

	internal static void SetPaginationData(this HttpContext ctx, IEnumerable<IEntity> entities)
	{
		ctx.Items.Add(Key, entities);
	}

	public static IEnumerable<IEntity>? GetPaginationData(this HttpContext ctx)
	{
		ctx.Items.TryGetValue(Key, out var entities);
		return entities as IEnumerable<IEntity>;
	}
}

public interface IPaginationQuery
{
	public string? MinId { get; }
	public int?    Limit { get; }
}