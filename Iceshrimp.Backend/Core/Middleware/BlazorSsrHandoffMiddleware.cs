using System.Collections.Concurrent;
using System.Reflection;
using Iceshrimp.Backend.Core.Extensions;
using Microsoft.AspNetCore.Components.Endpoints;

namespace Iceshrimp.Backend.Core.Middleware;

public class BlazorSsrHandoffMiddleware(RequestDelegate next) : IConditionalMiddleware
{
	private static readonly ConcurrentDictionary<Endpoint, bool> Cache = [];

	public async Task InvokeAsync(HttpContext context)
	{
		context.Response.OnStarting(() =>
		{
			context.Response.Headers.Remove("blazor-enhanced-nav");
			return Task.CompletedTask;
		});

		await next(context);
	}

	public static bool Predicate(HttpContext ctx)
		=> ctx.GetEndpoint() is { } endpoint &&
		   Cache.GetOrAdd(endpoint, e => e.Metadata.GetMetadata<RootComponentMetadata>()
		                                  ?.Type
		                                  .GetCustomAttributes<BlazorSsrAttribute>()
		                                  .Any() ??
		                                 false);

	public static void DisableBlazorJsInitializers(RazorComponentsServiceOptions options)
	{
		var property =
			options.GetType().GetProperty("JavaScriptInitializers", BindingFlags.Instance | BindingFlags.NonPublic) ??
			throw new Exception("Failed to disable Blazor JS initializers");

		property.SetValue(options, null);
	}
}

public class BlazorSsrAttribute : Attribute;