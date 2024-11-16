using System.Reflection;
using Microsoft.AspNetCore.Components.Endpoints;

namespace Iceshrimp.Backend.Core.Middleware;

public class BlazorSsrHandoffMiddleware : IMiddleware
{
	public async Task InvokeAsync(HttpContext context, RequestDelegate next)
	{
		var attribute = context.GetEndpoint()
		                       ?.Metadata.GetMetadata<RootComponentMetadata>()
		                       ?.Type.GetCustomAttributes<RazorSsrAttribute>()
		                       .FirstOrDefault();

		if (attribute != null)
		{
			context.Response.OnStarting(() =>
			{
				context.Response.Headers.Remove("blazor-enhanced-nav");
				return Task.CompletedTask;
			});
		}

		await next(context);
	}

	public static void DisableBlazorJsInitializers(RazorComponentsServiceOptions options)
	{
		var property =
			options.GetType().GetProperty("JavaScriptInitializers", BindingFlags.Instance | BindingFlags.NonPublic) ??
			throw new Exception("Failed to disable Blazor JS initializers");

		property.SetValue(options, null);
	}
}

public class RazorSsrAttribute : Attribute;