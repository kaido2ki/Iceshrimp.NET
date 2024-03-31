using Iceshrimp.Backend.Controllers.Mastodon.Schemas.Entities;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;

namespace Iceshrimp.Backend.Controllers.Mastodon.Renderers;

public static class FilterRenderer
{
	public static FilterEntity RenderOne(Filter filter)
	{
		var context = filter.Contexts.Select(c => c switch
		{
			Filter.FilterContext.Home          => "home",
			Filter.FilterContext.Lists         => "home",
			Filter.FilterContext.Threads       => "thread",
			Filter.FilterContext.Notifications => "notifications",
			Filter.FilterContext.Accounts      => "account",
			Filter.FilterContext.Public        => "public",
			_                                  => throw new ArgumentOutOfRangeException(nameof(c))
		});

		return new FilterEntity
		{
			Id           = filter.Id.ToString(),
			Keywords     = filter.Keywords.Select((p, i) => new FilterKeyword(p, filter.Id, i)).ToList(),
			Context      = context.Distinct().ToList(),
			Title        = filter.Name,
			ExpiresAt    = filter.Expiry?.ToStringIso8601Like(),
			FilterAction = filter.Action == Filter.FilterAction.Hide ? "hide" : "warn"
		};
	}
}