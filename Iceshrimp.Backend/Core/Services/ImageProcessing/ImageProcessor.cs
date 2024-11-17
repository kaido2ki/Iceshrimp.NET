using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Helpers;
using Microsoft.Extensions.Options;
using static Iceshrimp.Backend.Core.Services.ImageProcessing.ImageVersion;

namespace Iceshrimp.Backend.Core.Services.ImageProcessing;

public class ImageProcessor : ISingletonService
{
	private readonly IOptionsMonitor<Config.StorageSection> _config;
	//TODO: support stripping of exif/icc metadata (without re-encoding)

	private readonly List<IImageProcessor>   _imageProcessors;
	private readonly ILogger<ImageProcessor> _logger;
	private readonly SemaphorePlus           _semaphore;
	private readonly int                     _concurrency;

	public ImageProcessor(
		ILogger<ImageProcessor> logger, IOptionsMonitor<Config.StorageSection> config,
		IEnumerable<IImageProcessor> imageProcessors
	)
	{
		_logger          = logger;
		_config          = config;
		_imageProcessors = imageProcessors.OrderBy(p => p.Priority).ToList();
		_concurrency     = config.CurrentValue.MediaProcessing.ImageProcessorConcurrency;
		_semaphore       = new SemaphorePlus(Math.Max(_concurrency, 1));

		// @formatter:off
		if (_imageProcessors.Count == 0)
			_logger.LogInformation("Image processing is disabled as per the configuration.");
		else if (_imageProcessors.Count == 1)
			_logger.LogInformation("Using {processor} for image processing.", _imageProcessors[0].DisplayName);
		else
			_logger.LogInformation("Using [{processors}] for image processing.", string.Join(", ", _imageProcessors.Select(p => p.DisplayName)));
		// @formatter:on
	}

	public IImageInfo? IdentifyImage(byte[] buf, DriveFileCreationRequest request)
	{
		// @formatter:off
		var ident = RunProcessorAction("ident", p => p.Identify(buf), p => p.CanIdentify,
		                               () => throw new Exception("No available image processor supports identifying images"));
		// @formatter:on

		// Correct MIME type
		if ((request.MimeType == "image" && ident?.MimeType != null) || ident?.MimeType == "image/apng")
			request.MimeType = ident.MimeType;

		return ident;
	}

	public ProcessedImage ProcessImage(
		byte[] buf, IImageInfo ident, DriveFileCreationRequest request, IReadOnlyCollection<ImageVersion> formats
	)
	{
		if (_config.CurrentValue.MediaProcessing.ImageProcessor == Enums.ImageProcessor.None || formats.Count == 0)
			return new ProcessedImage(ident, new MemoryStream(buf), request);

		// @formatter:off
		var blurhash = RunProcessorAction("blurhash", p => p.Blurhash(buf, ident), p => p.CanGenerateBlurhash,
		                                  () => _logger.LogWarning("Skipping blurhash generation: No available image processor supports generating blurhashes"),
		                                  (p, e) => _logger.LogWarning("Failed to generate blurhash using {processor}: {e}", p, e));
		// @formatter:on

		var results = formats
		              .ToDictionary<ImageVersion, ImageVersion, Func<Task<Stream>>?>(p => p, ProcessImageFormat)
		              .AsReadOnly();

		return new ProcessedImage(ident) { RequestedFormats = results, Blurhash = blurhash };

		Func<Task<Stream>>? ProcessImageFormat(ImageVersion p)
		{
			if (p.Format is ImageFormat.Keep) return () => Task.FromResult<Stream>(new MemoryStream(buf));
			var proc = _imageProcessors.FirstOrDefault(i => i.CanEncode(p.Format));
			if (proc == null)
			{
				_logger.LogWarning("No image processor supports the format {format}, skipping", p.Format.MimeType);
				return null;
			}

			return async () =>
			{
				if (_concurrency is 0)
					return proc.Encode(buf, ident, p.Format);

				await _semaphore.WaitAsync();
				try
				{
					return proc.Encode(buf, ident, p.Format);
				}
				finally
				{
					_semaphore.Release();
				}
			};
		}
	}

	private T? RunProcessorAction<T>(
		string name, Func<IImageProcessor, T> action, Func<IImageProcessor, bool> locator,
		Action fallback, Action<IImageProcessor, Exception>? fallthrough = null
	) where T : class
	{
		var processors = _imageProcessors.Where(locator).ToImmutableArray();
		if (processors.Length == 0)
		{
			fallback();
			return null;
		}

		foreach (var processor in processors)
		{
			try
			{
				return action(processor);
			}
			catch (Exception e)
			{
				if (fallthrough != null)
				{
					fallthrough(processor, e);
				}
				else
				{
					_logger.LogWarning("Processor {name} failed to run {action}, falling through...",
					                   processor.DisplayName, name);
				}
			}
		}

		_logger.LogWarning("All processors failed to run {action}, returning null.", name);
		return null;
	}

	public class ProcessedImage : DriveFile.FileProperties
	{
		public string? Blurhash;

		public required IReadOnlyDictionary<ImageVersion, Func<Task<Stream>>?> RequestedFormats;

		public ProcessedImage(IImageInfo info)
		{
			Width  = info.Width;
			Height = info.Height;
		}

		[SetsRequiredMembers]
		public ProcessedImage(IImageInfo info, Stream original, DriveFileCreationRequest request) : this(info)
		{
			var format = new ImageFormat.Keep(Path.GetExtension(request.Filename).TrimStart('.'), request.MimeType);
			RequestedFormats = new Dictionary<ImageVersion, Func<Task<Stream>>?>
			{
				[new ImageVersion(KeyEnum.Original, format)] = () => Task.FromResult(original)
			};
		}
	}
}