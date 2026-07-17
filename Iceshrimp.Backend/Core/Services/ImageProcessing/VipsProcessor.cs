using CommunityToolkit.HighPerformance;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Helpers;
using NetVips;
using Iceshrimp.MimeTypes;
using Iceshrimp.Utils.DependencyInjection;
using SixLabors.ImageSharp.PixelFormats;
using Enums = NetVips.Enums;

namespace Iceshrimp.Backend.Core.Services.ImageProcessing;

public class VipsProcessor : ImageProcessorBase, IImageProcessor,
                             ISingletonService, IConditionalService<Config>, IService<IImageProcessor>
{
	private readonly ILogger<VipsProcessor> _logger;

	public bool CanIdentify         => true;
	public bool CanGenerateBlurhash => true;

	static int IService.Priority => -1;

	public static bool Predicate(Config ctx) =>
		ctx.Storage.MediaProcessing.ImageProcessor == Configuration.Enums.ImageProcessor.LibVips;

	public VipsProcessor(ILogger<VipsProcessor> logger) : base("LibVips", 0)
	{
		_logger = logger;

		//TODO: Implement something similar to https://github.com/lovell/sharp/blob/da655a1859744deec9f558effa5c9981ef5fd6d3/lib/utility.js#L153C5-L158
		NetVips.NetVips.Concurrency = 1;

		// We want to know when we have a memory leak
		NetVips.NetVips.Leak = true;

		// We don't need the VIPS operation or file cache
		Cache.Max      = 0;
		Cache.MaxFiles = 0;
		Cache.MaxMem   = 0;

		Log.SetLogHandler("VIPS", Enums.LogLevelFlags.Warning | Enums.LogLevelFlags.Error, VipsLogDelegate);
	}

	public bool CanEncode(ImageFormat format)
	{
		return format switch
		{
			ImageFormat.Webp => true,
			ImageFormat.Jxl  => true,
			ImageFormat.Avif => true,
			ImageFormat.Png  => true,
			_                => throw new ArgumentOutOfRangeException(nameof(format), format, null)
		};
	}

	public Stream Encode(byte[] input, IImageInfo _, ImageFormat format)
	{
		return format switch
		{
			ImageFormat.Webp opts => EncodeWebp(input, opts),
			ImageFormat.Jxl opts  => EncodeJxl(input, opts),
			ImageFormat.Avif opts => EncodeAvif(input, opts),
			ImageFormat.Png opts  => EncodePng(input, opts),
			_                     => throw new ArgumentOutOfRangeException(nameof(format))
		};
	}

	public string Blurhash(byte[] buf, IImageInfo ident)
	{
		using var blurhashImageSource =
			Image.ThumbnailBuffer(buf, width: 100, height: 100, size: Enums.Size.Down);
		using var blurhashImageRotated = blurhashImageSource.Autorot();
		using var blurhashImage = blurhashImageRotated.Interpretation == Enums.Interpretation.Srgb
			? blurhashImageRotated
			: blurhashImageRotated.Colourspace(Enums.Interpretation.Srgb);
		using var blurhashImageFlattened = blurhashImage.HasAlpha() ? blurhashImage.Flatten() : blurhashImage;
		using var blurhashImageActual    = blurhashImageFlattened.Cast(Enums.BandFormat.Uchar);

		var width  = blurhashImageActual.Width;
		var height = blurhashImageActual.Height;
		var pixels = width * height;

		if (width is < 1 or > 100) throw new VipsException($"Failed to compute blurhash: invalid width: {width}");
		if (height is < 1 or > 100) throw new VipsException($"Failed to compute blurhash: invalid height: {height}");

		var blurBuf = blurhashImageActual.WriteToMemory<byte>();
		var bufLen  = blurBuf.Length;

		var expectedBufLen = pixels * 3;
		if (bufLen != expectedBufLen) throw new VipsException("Failed to compute blurhash: buffer length mismatch");

		var         blurBufSpan   = blurBuf.AsSpan();
		Span<Rgb24> blurPixels = new Rgb24[pixels];

		for (var i = 0; i < pixels; i++)
		{
			var baseIdx = i * 3;
			blurPixels[i] = new Rgb24(blurBufSpan[baseIdx], blurBufSpan[baseIdx+1], blurBufSpan[baseIdx+2]);
		}

		return BlurhashHelper.Encode(blurPixels.AsSpan2D(height, width), 7, 7);
	}

	public IImageInfo Identify(byte[] input)
	{
		var image = Image.NewFromBuffer(input);
		if (!MimeTypeMap.TryGetMimeType(new MemoryStream(input), out var mime))
			mime = null;

		// Remove when https://github.com/libvips/libvips/issues/2537 is implemented
		if (mime == "image/png")
			mime = new ImageSharpProcessor.ImageSharpInfo(SixLabors.ImageSharp.Image.Identify(input)).MimeType;

		return new VipsImageInfo(image, mime);
	}

	private static MemoryStream EncodeWebp(byte[] buf, ImageFormat.Webp opts)
	{
		using var image  = Thumbnail(buf, opts.TargetRes);
		var       stream = new MemoryStream();
		image.WebpsaveStream(stream, opts.Quality, opts.Mode == ImageFormat.Webp.Compression.Lossless,
		                     nearLossless: opts.Mode == ImageFormat.Webp.Compression.NearLossless);
		return stream;
	}

	private static MemoryStream EncodeAvif(byte[] buf, ImageFormat.Avif opts)
	{
		using var image  = Thumbnail(buf, opts.TargetRes);
		var       stream = new MemoryStream();
		image.HeifsaveStream(stream, opts.Quality, lossless: opts.Mode == ImageFormat.Avif.Compression.Lossless,
		                     bitdepth: opts.BitDepth,
		                     compression: Enums.ForeignHeifCompression.Av1);
		return stream;
	}

	private static MemoryStream EncodeJxl(byte[] buf, ImageFormat.Jxl opts)
	{
		using var image  = Thumbnail(buf, opts.TargetRes);
		var       stream = new MemoryStream();
		image.JxlsaveStream(stream, q: opts.Quality, lossless: opts.Mode == ImageFormat.Jxl.Compression.Lossless,
		                    effort: opts.Effort);
		return stream;
	}

	private static MemoryStream EncodePng(byte[] buf, ImageFormat.Png opts)
	{
		using var image  = Thumbnail(buf, opts.TargetRes);
		var       stream = new MemoryStream();
		image.PngsaveStream(stream, opts.CompressionLevel);
		return stream;
	}

	private static Image StripMetadata(Image image)
	{
		using var intermediate = image.Autorot();
		return intermediate.Mutate(mutable =>
		{
			foreach (var field in mutable.GetFields())
			{
				if (field is "icc-profile-data") continue;
				mutable.Remove(field);
			}
		});
	}

	private static Image Thumbnail(byte[] buf, int targetRes)
	{
		using var image = Image.ThumbnailBuffer(buf, targetRes, height: targetRes, size: Enums.Size.Down);
		return StripMetadata(image);
	}

	private void VipsLogDelegate(string domain, Enums.LogLevelFlags _, string message) =>
		_logger.LogWarning("{domain} - {message}", domain, message);

	private static int GetPageCount(Image image)
	{
		if (!image.GetFields().Contains("n-pages")) return 1;
		try
		{
			return (image.Get("n-pages") as int?) switch
			{
				null      => 1,
				< 1       => 1,
				> 10000   => 1,
				{ } value => value
			};
		}
		catch (VipsException)
		{
			return 1;
		}
	}

	private class VipsImageInfo(Image image, string? mime) : IImageInfo
	{
		public int     Width      => image.Width;
		public int     Height     => image.Height;
		public bool    IsAnimated => GetPageCount(image) > 1 || mime is "image/apng";
		public string? MimeType   => mime;
	}
}