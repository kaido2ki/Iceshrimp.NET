using Microsoft.AspNetCore.Mvc.ActionConstraints;

namespace Iceshrimp.Backend.Controllers.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class MediaTypeRouteFilterAttribute(params string[] mediaTypes) : Attribute, IActionConstraint {
	public bool Accept(ActionConstraintContext context) {
		return context.RouteContext.HttpContext.Request.Headers.ContainsKey("Accept") &&
		       mediaTypes.Contains(context.RouteContext.HttpContext.Request.Headers.Accept.ToString());
	}

	public int Order => HttpMethodActionConstraint.HttpMethodConstraintOrder + 1;
}