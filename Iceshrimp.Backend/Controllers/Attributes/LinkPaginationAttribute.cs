using Iceshrimp.Backend.Controllers.Mastodon.Schemas;
using Iceshrimp.Backend.Core.Database;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Iceshrimp.Backend.Controllers.Attributes;

public class LinkPaginationAttribute(int defaultLimit, int maxLimit) : ActionFilterAttribute
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
		if (result.Value is not IEnumerable<IEntity> entities) return;
		var ids = entities.Select(p => p.Id).ToList();
		if (ids.Count == 0) return;
		if (query.MinId != null) ids.Reverse();

		List<string> links = [];

		var limit   = Math.Min(query.Limit ?? defaultLimit, maxLimit);
		var request = context.HttpContext.Request;

		if (ids.Count >= limit)
		{
			var next = new QueryBuilder { { "limit", limit.ToString() }, { "max_id", ids.Last() } };
			links.Add($"<{GetUrl(request, next.ToQueryString())}>; rel=\"next\"");
		}

		var prev = new QueryBuilder { { "limit", limit.ToString() }, { "min_id", ids.First() } };
		links.Add($"<{GetUrl(request, prev.ToQueryString())}>; rel=\"prev\"");

		context.HttpContext.Response.Headers.Link = string.Join(", ", links);
	}

	private static string GetUrl(HttpRequest request, QueryString query)
	{
		return UriHelper.BuildAbsolute("https", request.Host, request.PathBase, request.Path, query);
	}
}

public interface IPaginationQuery
{
	public string? MinId { get; }
	public int?    Limit { get; }
}