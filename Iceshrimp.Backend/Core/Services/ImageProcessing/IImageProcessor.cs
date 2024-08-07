namespace Iceshrimp.Backend.Core.Services.ImageProcessing;

public interface IImageProcessorBase
{
	public string DisplayName { get; }
	public int    Priority    { get; }
}

public interface IImageProcessor : IImageProcessorBase
{
	public bool CanIdentify         { get; }
	public bool CanGenerateBlurhash { get; }

	public IImageInfo Identify(byte[] input);
	public bool       CanEncode(ImageFormat format);
	public Stream     Encode(byte[] input, IImageInfo ident, ImageFormat format);
	public string     Blurhash(byte[] input, IImageInfo ident);
}

public abstract class ImageProcessorBase(string displayName, int priority) : IImageProcessorBase
{
	public string DisplayName => displayName;
	public int    Priority    => priority;
}