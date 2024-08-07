using CommunityToolkit.HighPerformance;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Helpers;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Memory;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using ImageSharpConfig = SixLabors.ImageSharp.Configuration;

namespace Iceshrimp.Backend.Core.Services.ImageProcessing;

public class ImageSharpProcessor : ImageProcessorBase, IImageProcessor
{
	private readonly ILogger<ImageSharpProcessor> _logger;
	private readonly ImageSharpConfig             _sharpConfig;
	private readonly ImageSharpConfig             _sharpConfigContiguous;

	public bool CanIdentify         => true;
	public bool CanGenerateBlurhash => true;

	public ImageSharpProcessor(
		ILogger<ImageSharpProcessor> logger, IOptions<Config.StorageSection> config
	) : base("ImageSharp", 1)
	{
		_logger      = logger;
		_sharpConfig = ImageSharpConfig.Default.Clone();

		// @formatter:off
		_sharpConfig.MemoryAllocator = MemoryAllocator.Create(new MemoryAllocatorOptions
		{
			// 1MP / 1000000 px * 4 channels (RGBA) * 8 bits per channel / 8 bit per byte / 1024 byte per kb / 1024 kb per mb
			// This works out to ~3.85MB per Mpx, so 4 leaves a bit of buffer.
			AllocationLimitMegabytes = config.Value.MediaProcessing.MaxResolutionMpx * 4
		});

		_sharpConfigContiguous = _sharpConfig.Clone();
		_sharpConfigContiguous.PreferContiguousImageBuffers = true;
		// @formatter:on
	}

	public IImageInfo Identify(byte[] input)
	{
		return new ImageSharpInfo(Image.Identify(input));
	}

	public bool CanEncode(ImageFormat format)
	{
		return format switch
		{
			ImageFormat.Webp => true,
			ImageFormat.Jxl  => false,
			ImageFormat.Avif => false,
			_                => throw new ArgumentOutOfRangeException(nameof(format), format, null)
		};
	}

	public Stream Encode(byte[] input, IImageInfo ident, ImageFormat format)
	{
		return format switch
		{
			ImageFormat.Webp opts => EncodeWebp(input, ident, opts),
			_                     => throw new ArgumentOutOfRangeException(nameof(format))
		};
	}

	private Stream EncodeWebp(byte[] data, IImageInfo ident, ImageFormat.Webp opts)
	{
		using var image = GetImage<Rgba32>(data, ident, opts.TargetRes);
		var thumbEncoder = new WebpEncoder
		{
			Quality = opts.Quality,
			FileFormat = opts.Mode == ImageFormat.Webp.Compression.Lossless
				? WebpFileFormatType.Lossless
				: WebpFileFormatType.Lossy,
			NearLossless = opts.Mode == ImageFormat.Webp.Compression.NearLossless
		};

		var stream = new MemoryStream();
		image.SaveAsWebp(stream, thumbEncoder);
		return stream;
	}

	public string Blurhash(byte[] data, IImageInfo ident)
	{
		using var image = GetImage<Rgb24>(data, ident, 100, preferContiguous: true);
		return Blurhash(image);
	}

	// Since we can't work with Span<T> objects in async blocks, this needs to be done in a separate method.
	private string Blurhash(Image<Rgb24> image)
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

	private Image<TPixel> GetImage<TPixel>(
		byte[] data, IImageInfo ident, int width, int? height = null, bool preferContiguous = false
	) where TPixel : unmanaged, IPixel<TPixel>
	{
		width  = Math.Min(ident.Width, width);
		height = Math.Min(ident.Height, height ?? width);
		var size = new Size(width, height.Value);
		var options = new DecoderOptions
		{
			MaxFrames     = 1,
			TargetSize    = size,
			Configuration = preferContiguous ? _sharpConfigContiguous : _sharpConfig
		};

		var image = Image.Load<TPixel>(options, data);
		image.Mutate(x => x.AutoOrient());
		image.Metadata.ExifProfile = null;
		var opts = new ResizeOptions { Size = size, Mode = ResizeMode.Max };
		image.Mutate(p => p.Resize(opts));
		return image;
	}

	private class ImageSharpInfo(ImageInfo info) : IImageInfo
	{
		public int  Width      => info.Width;
		public int  Height     => info.Height;
		public bool IsAnimated => info.IsAnimated || info.FrameMetadataCollection.Count != 0;

		public string? MimeType
		{
			get
			{
				if (info.Metadata.DecodedImageFormat is PngFormat && info.IsAnimated)
					return "image/apng";
				return info.Metadata.DecodedImageFormat?.DefaultMimeType;
			}
		}

		public static implicit operator ImageSharpInfo(ImageInfo src) => new(src);
	}
}