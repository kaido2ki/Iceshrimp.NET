using AngleSharp.Io;
using Iceshrimp.Backend.Core.Middleware;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace Iceshrimp.Backend.Core.Extensions;

public static class WebApplicationBlazorFrameworkExtensions
{
	private static readonly string? DotnetModifiableAssemblies =
		GetNonEmptyEnvironmentVariableValue("DOTNET_MODIFIABLE_ASSEMBLIES");

	private static readonly string? AspnetcoreBrowserTools =
		GetNonEmptyEnvironmentVariableValue("__ASPNETCORE_BROWSER_TOOLS");

	private static string? GetNonEmptyEnvironmentVariableValue(string name)
	{
		var environmentVariable = Environment.GetEnvironmentVariable(name);
		return environmentVariable is not { Length: > 0 } ? null : environmentVariable;
	}

	private static void AddMapping(
		this FileExtensionContentTypeProvider provider,
		string name,
		string mimeType
	)
	{
		provider.Mappings.TryAdd(name, mimeType);
	}

	private static StaticFileOptions CreateStaticFilesOptions(IFileProvider webRootFileProvider)
	{
		var staticFilesOptions  = new StaticFileOptions { FileProvider = webRootFileProvider };
		var contentTypeProvider = new FileExtensionContentTypeProvider();

		contentTypeProvider.AddMapping(".dll", "application/octet-stream");
		contentTypeProvider.AddMapping(".webcil", "application/octet-stream");
		contentTypeProvider.AddMapping(".pdb", "application/octet-stream");
		contentTypeProvider.AddMapping(".br", "application/octet-stream");
		contentTypeProvider.AddMapping(".dat", "application/octet-stream");
		contentTypeProvider.AddMapping(".blat", "application/octet-stream");

		staticFilesOptions.ContentTypeProvider = contentTypeProvider;
		staticFilesOptions.OnPrepareResponse = (Action<StaticFileResponseContext>)(fileContext =>
		{
			fileContext.Context.Response.Headers.Append(HeaderNames.CacheControl, (StringValues)"no-cache");
			var path      = fileContext.Context.Request.Path;
			var extension = Path.GetExtension(path.Value);
			if (!string.Equals(extension, ".gz") && !string.Equals(extension, ".br"))
				return;
			var withoutExtension = Path.GetFileNameWithoutExtension(path.Value);
			if (withoutExtension == null ||
			    !contentTypeProvider.TryGetContentType(withoutExtension, out var contentType))
				return;
			fileContext.Context.Response.ContentType = contentType;
		});
		return staticFilesOptions;
	}

	public static IApplicationBuilder UseBlazorFrameworkFilesWithTransparentDecompression(this IApplicationBuilder app)
	{
		var webHostEnvironment = app.ApplicationServices.GetRequiredService<IWebHostEnvironment>();
		var options            = CreateStaticFilesOptions(webHostEnvironment.WebRootFileProvider);

		app.MapWhen(Predicate, subBuilder =>
		{
			subBuilder.Use(async (context, next) =>
			{
				context.Response.Headers.Append("Blazor-Environment", webHostEnvironment.EnvironmentName);
				if (AspnetcoreBrowserTools != null)
					context.Response.Headers.Append("ASPNETCORE-BROWSER-TOOLS", AspnetcoreBrowserTools);
				if (DotnetModifiableAssemblies != null)
					context.Response.Headers.Append("DOTNET-MODIFIABLE-ASSEMBLIES",
					                                DotnetModifiableAssemblies);
				await next(context);
			});
			subBuilder.UseMiddleware<ContentEncodingNegotiator>();
			subBuilder.UseStaticFiles(options);
			subBuilder.Use(async (HttpContext context, RequestDelegate _) =>
			{
				context.Response.StatusCode = 404;
				await context.Response.StartAsync();
			});
		});

		return app;

		bool Predicate(HttpContext ctx) => ctx.Request.Path.StartsWithSegments(new PathString(), out var remaining) &&
		                                   remaining.StartsWithSegments((PathString)"/_framework") &&
		                                   !remaining.StartsWithSegments((PathString)"/_framework/blazor.server.js") &&
		                                   !remaining.StartsWithSegments((PathString)"/_framework/blazor.web.js");
	}
}