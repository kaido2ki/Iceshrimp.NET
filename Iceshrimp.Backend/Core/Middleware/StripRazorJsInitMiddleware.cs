using System.Reflection;
using Microsoft.AspNetCore.Components.Endpoints;

namespace Iceshrimp.Backend.Core.Middleware;

public class StripRazorJsInitMiddleware : IMiddleware
{
	private static readonly byte[] Magic = "<!--Blazor-Web-Initializers"u8.ToArray();

	public async Task InvokeAsync(HttpContext context, RequestDelegate next)
	{
		var attribute = context.GetEndpoint()
		                       ?.Metadata.GetMetadata<RootComponentMetadata>()
		                       ?.Type.GetCustomAttributes<RazorSsrAttribute>()
		                       .FirstOrDefault();

		if (attribute == null)
		{
			await next(context);
			return;
		}

		var body   = context.Response.Body;
		var stream = new MemoryStream();
		context.Response.Body = stream;

		try
		{
			await next(context);

			stream.Seek(0, SeekOrigin.Begin);
			if (stream.TryGetBuffer(out var buffer))
			{
				var index = buffer.AsSpan().IndexOf(Magic);
				if (index != -1)
					stream.SetLength(index);
			}

			context.Response.Headers.ContentLength = stream.Length;
			await stream.CopyToAsync(body);
		}
		finally
		{
			// Revert body stream
			context.Response.Body = body;
		}
	}
}

public class RazorSsrAttribute : Attribute;