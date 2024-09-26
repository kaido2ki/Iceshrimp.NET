using AngleSharp.Io;
using Microsoft.AspNetCore.Routing.Matching;

namespace Iceshrimp.Backend.Components.PublicPreview.Attributes;

public class PublicPreviewRouteMatcher : MatcherPolicy, IEndpointSelectorPolicy
{
	public override int Order => 99999; // That's ActionConstraintMatcherPolicy - 1

	public bool AppliesToEndpoints(IReadOnlyList<Endpoint> endpoints)
	{
		return endpoints.Any(p => p.Metadata.GetMetadata<PublicPreviewRouteFilterAttribute>() != null);
	}

	public Task ApplyAsync(HttpContext ctx, CandidateSet candidates)
	{
		var applies = Enumerate(candidates)
			.Any(p => p.Score >= 0 && p.Endpoint.Metadata.GetMetadata<PublicPreviewRouteFilterAttribute>() != null);
		if (!applies) return Task.CompletedTask;

		// Add Vary: Accept to the response headers to prevent caches serving the wrong response
		ctx.Response.Headers.Append(HeaderNames.Vary, HeaderNames.Cookie);

		var hasCookie = ctx.Request.Cookies.ContainsKey("sessions");
		for (var i = 0; i < candidates.Count; i++)
		{
			var candidate = candidates[i];
			var hasAttr   = candidate.Endpoint.Metadata.GetMetadata<PublicPreviewRouteFilterAttribute>() != null;
			candidates.SetValidity(i, !hasCookie || !hasAttr);
		}

		return Task.CompletedTask;
	}

	private static IEnumerable<CandidateState> Enumerate(CandidateSet candidates)
	{
		for (var i = 0; i < candidates.Count; i++) yield return candidates[i];
	}
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class PublicPreviewRouteFilterAttribute : Attribute;