using System.IO.Compression;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using HeaderNames = AngleSharp.Io.HeaderNames;

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

	private sealed class ContentEncodingNegotiator(RequestDelegate next, IWebHostEnvironment webHostEnvironment)
	{
		private static readonly StringSegment[] PreferredEncodings = ["br", "gzip"];

		private static readonly Dictionary<StringSegment, string> EncodingExtensionMap =
			new(StringSegmentComparer.OrdinalIgnoreCase) { ["br"] = ".br", ["gzip"] = ".gz" };

		public Task InvokeAsync(HttpContext context)
		{
			NegotiateEncoding(context);
			return HookTransparentDecompression(context);
		}

		private async Task HookTransparentDecompression(HttpContext ctx)
		{
			if (ctx.Response.Headers.ContentEncoding.Count != 0 ||
			    !ResourceExists(ctx, ".br") ||
			    ResourceExists(ctx, ""))
			{
				await next(ctx);
				return;
			}

			var             responseStream = ctx.Response.Body;
			using var       tempStream     = new MemoryStream();
			await using var brotliStream   = new BrotliStream(tempStream, CompressionMode.Decompress);

			ctx.Response.Body = tempStream;
			ctx.Request.Path  = (PathString)(ctx.Request.Path + ".br");

			ctx.Response.OnStarting(() =>
			{
				ctx.Response.Headers.ContentLength = null;
				return Task.CompletedTask;
			});

			await next(ctx);

			tempStream.Seek(0, SeekOrigin.Begin);
			await brotliStream.CopyToAsync(responseStream);
		}

		private void NegotiateEncoding(HttpContext context)
		{
			var acceptEncoding = context.Request.Headers.AcceptEncoding;
			if (StringValues.IsNullOrEmpty(acceptEncoding) ||
			    !StringWithQualityHeaderValue.TryParseList(acceptEncoding, out var parsedValues) ||
			    parsedValues.Count == 0)
				return;
			var stringSegment1 = StringSegment.Empty;
			var num            = 0.0;
			foreach (var encoding in parsedValues)
			{
				var stringSegment2 = encoding.Value;
				var valueOrDefault = encoding.Quality.GetValueOrDefault(1.0);
				if (!(valueOrDefault >= double.Epsilon) || !(valueOrDefault >= num)) continue;

				if (Math.Abs(valueOrDefault - num) < 0.001)
				{
					stringSegment1 = PickPreferredEncoding(context, stringSegment1, encoding);
				}
				else
				{
					if (EncodingExtensionMap.TryGetValue(stringSegment2, out var extension) &&
					    ResourceExists(context, extension))
					{
						stringSegment1 = stringSegment2;
						num            = valueOrDefault;
					}
				}

				if (StringSegment.Equals("*", stringSegment2, StringComparison.Ordinal))
				{
					stringSegment1 = PickPreferredEncoding(context, new StringSegment(), encoding);
					num            = valueOrDefault;
				}

				if (!StringSegment.Equals("identity", stringSegment2, StringComparison.OrdinalIgnoreCase))
					continue;

				stringSegment1 = StringSegment.Empty;
				num            = valueOrDefault;
			}

			if (!EncodingExtensionMap.TryGetValue(stringSegment1, out var str))
				return;

			context.Request.Path                     += str;
			context.Response.Headers.ContentEncoding =  stringSegment1.Value;
			context.Response.Headers.Append(HeaderNames.Vary, HeaderNames.ContentEncoding);
			return;

			StringSegment PickPreferredEncoding(
				HttpContext innerContext,
				StringSegment selectedEncoding,
				StringWithQualityHeaderValue encoding
			)
			{
				foreach (var preferredEncoding in PreferredEncodings)
				{
					if (preferredEncoding == selectedEncoding)
						return selectedEncoding;
					if ((preferredEncoding == encoding.Value || encoding.Value == "*") &&
					    ResourceExists(innerContext, EncodingExtensionMap[preferredEncoding]))
						return preferredEncoding;
				}

				return StringSegment.Empty;
			}
		}

		private bool ResourceExists(HttpContext context, string extension)
		{
			return webHostEnvironment.WebRootFileProvider.GetFileInfo(context.Request.Path + extension).Exists;
		}
	}
}