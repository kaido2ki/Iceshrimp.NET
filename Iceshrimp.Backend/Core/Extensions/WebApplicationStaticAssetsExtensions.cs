using System.IO.Compression;
using Microsoft.AspNetCore.StaticAssets;

namespace Iceshrimp.Backend.Core.Extensions;

public static class WebApplicationStaticAssetsExtensions
{
	public static void MapStaticAssetsWithTransparentDecompression(this WebApplication app)
	{
		app.MapStaticAssets()
		   .Finally(builder =>
		   {
			   var @delegate = builder.RequestDelegate;
			   builder.RequestDelegate = async ctx =>
			   {
				   HookTransparentDecompression(ctx);
				   if (@delegate?.Invoke(ctx) is { } task)
					   await task;
			   };
		   });
	}

	private static void HookTransparentDecompression(HttpContext ctx)
	{
		if (ctx.GetEndpoint()?.Metadata.GetMetadata<StaticAssetDescriptor>() is not { } descriptor) return;
		if (descriptor.AssetPath == descriptor.Route) return;
		if (!descriptor.AssetPath.EndsWith(".br")) return;
		if (descriptor.Selectors is not []) return;

		var body       = ctx.Response.Body;
		var compressed = new MemoryStream();
		ctx.Response.Body = compressed;

		ctx.Response.OnStarting(async () =>
		{
			int? length = null;
			var  desc   = descriptor.Properties.FirstOrDefault(p => p.Name == "Uncompressed-Length")?.Value;
			if (int.TryParse(desc, out var parsed))
				length = parsed;

			try
			{
				ctx.Response.Headers.ContentLength   = length;
				ctx.Response.Headers.ContentEncoding = "plain";

				await using var brotli = new BrotliStream(compressed, CompressionMode.Decompress);
				compressed.Seek(0, SeekOrigin.Begin);
				await brotli.CopyToAsync(body);
			}
			finally
			{
				await compressed.DisposeAsync();
			}
		});
	}
}