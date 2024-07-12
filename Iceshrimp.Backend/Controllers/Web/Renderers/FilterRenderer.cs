using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Shared.Schemas.Web;

namespace Iceshrimp.Backend.Controllers.Web.Renderers;

public static class FilterRenderer
{
	public static FilterResponse RenderOne(Filter filter) => new()
	{
		Id       = filter.Id,
		Name     = filter.Name,
		Expiry   = filter.Expiry,
		Keywords = filter.Keywords,
		Action   = (FilterResponse.FilterAction)filter.Action,
		Contexts = filter.Contexts.Cast<FilterResponse.FilterContext>().ToList(),
	};

	public static IEnumerable<FilterResponse> RenderMany(IEnumerable<Filter> filters) => filters.Select(RenderOne);
}