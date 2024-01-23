using Microsoft.AspNetCore.Mvc.ActionConstraints;

namespace Iceshrimp.Backend.Controllers.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class MediaTypeRouteFilterAttribute(params string[] mediaTypes) : Attribute, IActionConstraint {
	public bool Accept(ActionConstraintContext context) {
		//TODO: this should parse the header properly, edge cases like profile=, charset=, q= are not currently handled.
		//TODO: this should set the correct content type for the response as well
		return context.RouteContext.HttpContext.Request.Headers.ContainsKey("Accept") &&
		       mediaTypes.Any(p => context.RouteContext.HttpContext.Request.Headers.Accept.ToString() == p ||
		                           context.RouteContext.HttpContext.Request.Headers.Accept.ToString()
		                                  .StartsWith(p + ";"));
	}

	public int Order => HttpMethodActionConstraint.HttpMethodConstraintOrder + 1;
}