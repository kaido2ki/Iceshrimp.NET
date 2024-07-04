using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.Net.Http.Headers;

namespace Iceshrimp.Backend.Controllers.Federation.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class MediaTypeRouteFilterAttribute(params string[] mediaTypes) : Attribute, IActionConstraint
{
	public bool Accept(ActionConstraintContext context)
	{
		// Add Vary: Accept to the response headers to prevent caches serving the wrong response
		context.RouteContext.HttpContext.Response.Headers.Append(HeaderNames.Vary, HeaderNames.Accept);

		if (!context.RouteContext.HttpContext.Request.Headers.ContainsKey("Accept")) return false;

		var accept = context.RouteContext.HttpContext.Request.Headers.Accept.ToString()
		                    .Split(',')
		                    .Select(MediaTypeWithQualityHeaderValue.Parse)
		                    .Select(p => p.MediaType);

		return accept.Any(mediaTypes.Contains);
	}

	public int Order => HttpMethodActionConstraint.HttpMethodConstraintOrder + 1;
}