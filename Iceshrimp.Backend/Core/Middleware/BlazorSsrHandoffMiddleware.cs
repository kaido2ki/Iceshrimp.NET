using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using Iceshrimp.Utils.DependencyInjection;
using Microsoft.AspNetCore.Components.Endpoints;

namespace Iceshrimp.Backend.Core.Middleware;

public class BlazorSsrHandoffMiddleware(RequestDelegate next) : IConditionalMiddleware
{
	private static readonly   ConcurrentDictionary<Endpoint, bool> Cache = [];

	[UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_JavaScriptInitializers")]
	private static extern void SetJsInitializers(RazorComponentsServiceOptions options, string? value);

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
		SetJsInitializers(options, null);
	}
}

public class BlazorSsrAttribute : Attribute;