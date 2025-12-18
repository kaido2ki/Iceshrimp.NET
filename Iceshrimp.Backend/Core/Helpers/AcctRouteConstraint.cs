namespace Iceshrimp.Backend.Core.Helpers;

public class AcctRouteConstraint : IRouteConstraint
{
	public bool Match(
		HttpContext? httpContext, IRouter? route, string routeKey, RouteValueDictionary values,
		RouteDirection routeDirection
	)
	{
		if (!values.TryGetValue(routeKey, out var routeValue) || routeValue is not string acct) return false;
		if (!acct.StartsWith('@')) return false;
		var split = acct.Split('@');
		return split.Length is 2 or 3;
	}
}
