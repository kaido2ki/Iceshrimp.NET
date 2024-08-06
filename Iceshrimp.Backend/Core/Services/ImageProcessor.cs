using System.Runtime.InteropServices;
using CommunityToolkit.HighPerformance;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Helpers;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Memory;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using ImageSharp = SixLabors.ImageSharp.Image;

namespace Iceshrimp.Backend.Core.Services;

public class ImageProcessor
{
	private readonly ILogger<ImageProcessor>                _logger;
	private readonly IOptionsMonitor<Config.StorageSection> _config;

	public ImageProcessor(ILogger<ImageProcessor> logger, IOptionsMonitor<Config.StorageSection> config)
	{
		_logger = logger;
		_config = config;

		if (config.CurrentValue.MediaProcessing.ImageProcessor == Enums.ImageProcessor.None)
		{
			_logger.LogInformation("Image processing is disabled as per the configuration.");
			return;
		}

		SixLabors.ImageSharp.Configuration.Default.MemoryAllocator = MemoryAllocator.Create(new MemoryAllocatorOptions
		{
			// 1MP / 1000000 px * 4 channels (RGBA) * 8 bits per channel / 8 bit per byte / 1024 byte per kb / 1024 kb per mb
			// This works out to ~3.85MB per Mpx, so 4 leaves a bit of buffer.
			AllocationLimitMegabytes = config.CurrentValue.MediaProcessing.MaxResolutionMpx * 4
		});

		#if EnableLibVips
		if (_config.CurrentValue.MediaProcessing.ImageProcessor != Enums.ImageProcessor.LibVips)
		{
			_logger.LogDebug("VIPS support was enabled at compile time, but is not enabled in the configuration, skipping VIPS init");
			_logger.LogInformation("Using ImageSharp for image processing.");
			return;
		}

		//TODO: Implement something similar to https://github.com/lovell/sharp/blob/da655a1859744deec9f558effa5c9981ef5fd6d3/lib/utility.js#L153C5-L158
		NetVips.NetVips.Concurrency = 1;

		// We want to know when we have a memory leak
		NetVips.NetVips.Leak = true;

		// We don't need the VIPS operation or file cache
		NetVips.Cache.Max = 0;
		NetVips.Cache.MaxFiles = 0;
		NetVips.Cache.MaxMem = 0;

		NetVips.Log.SetLogHandler("VIPS", NetVips.Enums.LogLevelFlags.Warning | NetVips.Enums.LogLevelFlags.Error,
		                          VipsLogDelegate);
		_logger.LogInformation("Using VIPS for image processing.");
		#else
		if (config.CurrentValue.MediaProcessing.ImageProcessor == Enums.ImageProcessor.LibVips)
		{
			_logger.LogWarning("VIPS support was disabled at compile time, but ImageProcessor is set to LibVips in the configuration. Either compile with -p:EnableLibVips=true, or set the ImageProcessor configuration option to something else.");
		}
		else
		{
			_logger.LogDebug("VIPS support was disabled at compile time, skipping VIPS init");
		}

		_logger.LogInformation("Using ImageSharp for image processing.");
		#endif
	}

	public class Result
	{
		public string? Blurhash;

		public required DriveFile.FileProperties Properties;
		public          Func<Stream, Task>?      RenderThumbnail;
		public          Func<Stream, Task>?      RenderWebpublic;
	}

	public async Task<Result?> ProcessImage(Stream data, DriveFileCreationRequest request, bool genThumb, bool genWebp)
	{
		try
		{
			var pre   = DateTime.Now;
			var ident = await ImageSharp.IdentifyAsync(data);
			data.Seek(0, SeekOrigin.Begin);

			Result? res = null;

			// Correct mime type
			if (request.MimeType == "image" && ident.Metadata.DecodedImageFormat?.DefaultMimeType != null)
				request.MimeType = ident.Metadata.DecodedImageFormat.DefaultMimeType;
			if (ident.Metadata.DecodedImageFormat is PngFormat && ident.IsAnimated)
				request.MimeType = "image/apng";

			if (_config.CurrentValue.MediaProcessing.ImageProcessor == Enums.ImageProcessor.None)
			{
				var props = new DriveFile.FileProperties { Width = ident.Size.Width, Height = ident.Size.Height };
				return new Result { Properties = props };
			}

			// Don't generate thumb/webp for animated images
			if (ident.FrameMetadataCollection.Count != 0 || ident.IsAnimated)
			{
				genThumb = false;
				genWebp  = false;
			}

			if (ident.Width * ident.Height > _config.CurrentValue.MediaProcessing.MaxResolutionMpx * 1000 * 1000)
			{
				_logger.LogDebug("Image is larger than {mpx}mpx ({width}x{height}), bypassing image processing pipeline",
				                 _config.CurrentValue.MediaProcessing.MaxResolutionMpx, ident.Width, ident.Height);
				var props = new DriveFile.FileProperties { Width = ident.Size.Width, Height = ident.Size.Height };
				return new Result { Properties = props };
			}

			#if EnableLibVips
			if (_config.CurrentValue.MediaProcessing.ImageProcessor == Enums.ImageProcessor.LibVips)
			{
				try
				{
					byte[] buf;
					await using (var memoryStream = new MemoryStream())
					{
						await data.CopyToAsync(memoryStream);
						buf = memoryStream.ToArray();
					}

					res = await ProcessImageVips(buf, ident, request, genThumb, genWebp);
				}
				catch (Exception e)
				{
					_logger.LogWarning("Failed to process image of type {type} with VIPS, falling back to ImageSharp: {e}",
					                   request.MimeType, e.Message);
				}
			}
			#endif

			try
			{
				res ??= await ProcessImageSharp(data, ident, request, genThumb, genWebp);
			}
			catch (Exception e)
			{
				_logger.LogWarning("Failed to process image of type {type} with ImageSharp: {e}",
				                   request.MimeType, e.Message);
				var props = new DriveFile.FileProperties { Width = ident.Size.Width, Height = ident.Size.Height };
				return new Result { Properties = props };
			}

			_logger.LogTrace("Image processing took {ms} ms", (int)(DateTime.Now - pre).TotalMilliseconds);
			return res;
		}
		catch (Exception e)
		{
			_logger.LogError("Failed to process image with mime type {type}: {e}",
			                 request.MimeType, e.Message);
			return null;
		}
	}

	private async Task<Result> ProcessImageSharp(
		Stream data, ImageInfo ident, DriveFileCreationRequest request, bool genThumb, bool genWebp
	)
	{
		var properties = new DriveFile.FileProperties { Width = ident.Size.Width, Height = ident.Size.Height };
		var res        = new Result { Properties              = properties };
		// Calculate blurhash using a x100px image for improved performance
		using (var image = await GetImage<Rgb24>(data, ident, 100, preferContiguous: true))
		{
			res.Blurhash = GetBlurhashImageSharp(image);
		}

		if (genThumb)
		{
			res.RenderThumbnail = async stream =>
			{
				using var image        = await GetImage<Rgba32>(data, ident, 1000);
				var       thumbEncoder = new WebpEncoder { Quality = 75, FileFormat = WebpFileFormatType.Lossy };
				await image.SaveAsWebpAsync(stream, thumbEncoder);
			};
		}

		if (genWebp)
		{
			res.RenderWebpublic = async stream =>
			{
				using var image        = await GetImage<Rgba32>(data, ident, 2048);
				var       q            = request.MimeType == "image/png" ? 100 : 75;
				var       thumbEncoder = new WebpEncoder { Quality = q, FileFormat = WebpFileFormatType.Lossy };
				await image.SaveAsWebpAsync(stream, thumbEncoder);
			};
		}

		return res;
	}

	// Since we can't work with Span<T> objects in async blocks, this needs to be done in a separate method.
	private string GetBlurhashImageSharp(Image<Rgb24> image)
	{
		Span<Rgb24> span;
		if (image.DangerousTryGetSinglePixelMemory(out var mem))
		{
			span = mem.Span;
		}
		else
		{
			_logger.LogWarning("Failed to generate blurhash using ImageSharp: Memory region not contiguous. Falling back to block copy...");
			span = new Rgb24[image.Width * image.Height];
			image.CopyPixelDataTo(span);
		}

		return BlurhashHelper.Encode(span.AsSpan2D(image.Height, image.Width), 7, 7);
	}

	private static async Task<Image<TPixel>> GetImage<TPixel>(
		Stream data, ImageInfo ident, int width, int? height = null, bool preferContiguous = false
	) where TPixel : unmanaged, IPixel<TPixel>
	{
		width  = Math.Min(ident.Width, width);
		height = Math.Min(ident.Height, height ?? width);
		var size = new Size(width, height.Value);
		var config = preferContiguous
			? SixLabors.ImageSharp.Configuration.Default.Clone()
			: SixLabors.ImageSharp.Configuration.Default;

		if (preferContiguous)
			config.PreferContiguousImageBuffers = true;

		var options = new DecoderOptions
		{
			MaxFrames     = 1,
			TargetSize    = size,
			Configuration = config
		};

		data.Seek(0, SeekOrigin.Begin);
		var image = await ImageSharp.LoadAsync<TPixel>(options, data);
		image.Mutate(x => x.AutoOrient());
		var opts = new ResizeOptions { Size = size, Mode = ResizeMode.Max };
		image.Mutate(p => p.Resize(opts));
		return image;
	}

	#if EnableLibVips
	private static Task<Result> ProcessImageVips(
		byte[] buf, ImageInfo ident, DriveFileCreationRequest request, bool genThumb, bool genWebp
	)
	{
		var properties = new DriveFile.FileProperties { Width = ident.Size.Width, Height = ident.Size.Height };
		var res = new Result { Properties = properties };

		// Calculate blurhash using a x100px image for improved performance
		using var blurhashImageSource =
			NetVips.Image.ThumbnailBuffer(buf, width: 100, height: 100, size: NetVips.Enums.Size.Down);
		using var blurhashImage = blurhashImageSource.Interpretation == NetVips.Enums.Interpretation.Srgb
			? blurhashImageSource
			: blurhashImageSource.Colourspace(NetVips.Enums.Interpretation.Srgb);
		using var blurhashImageFlattened = blurhashImage.HasAlpha() ? blurhashImage.Flatten() : blurhashImage;
		using var blurhashImageActual = blurhashImageFlattened.Cast(NetVips.Enums.BandFormat.Uchar);

		var blurBuf = blurhashImageActual.WriteToMemory();
		var blurPixels = MemoryMarshal.Cast<byte, Rgb24>(blurBuf).AsSpan2D(blurhashImage.Height, blurhashImage.Width);
		res.Blurhash = BlurhashHelper.Encode(blurPixels, 7, 7);

		if (genThumb)
		{
			res.RenderThumbnail = stream =>
			{
				using var thumbnailImage =
					NetVips.Image.ThumbnailBuffer(buf, width: 1000, height: 1000, size: NetVips.Enums.Size.Down);
				thumbnailImage.WebpsaveStream(stream, 75, false);
				return Task.CompletedTask;
			};

			// Generate webpublic for local users, if image is not animated
			if (genWebp)
			{
				res.RenderWebpublic = stream =>
				{
					using var webpublicImage =
						NetVips.Image.ThumbnailBuffer(buf, width: 2048, height: 2048,
						                              size: NetVips.Enums.Size.Down);
					webpublicImage.WebpsaveStream(stream, request.MimeType == "image/png" ? 100 : 75, false);
					return Task.CompletedTask;
				};
			}
		}

		return Task.FromResult(res);
	}

	private void VipsLogDelegate(string domain, NetVips.Enums.LogLevelFlags _, string message) =>
		_logger.LogWarning("{domain} - {message}", domain, message);
	#endif
}