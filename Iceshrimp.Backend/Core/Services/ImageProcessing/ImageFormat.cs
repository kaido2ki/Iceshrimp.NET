using System.ComponentModel.DataAnnotations;

namespace Iceshrimp.Backend.Core.Services.ImageProcessing;

public abstract record ImageFormat
{
	public string Extension { get; init; }
	public string MimeType  { get; init; }

	private ImageFormat(string Extension, string MimeType)
	{
		this.Extension = Extension;
		this.MimeType  = MimeType;
	}

	public void Deconstruct(out string extension, out string mimeType)
	{
		extension = Extension;
		mimeType  = MimeType;
	}

	public record Keep(string Extension, string MimeType) : ImageFormat(Extension, MimeType);
	//TODO: public record StripExifAndIcc(string Extension, string MimeType) : ImageFormat(Extension, MimeType);

	public record Webp(
		Webp.Compression Mode,
		[Range(0, 100)] int Quality,
		int TargetRes
	) : ImageFormat("webp", "image/webp")
	{
		public enum Compression
		{
			Lossy,
			NearLossless,
			Lossless
		}
	}

	public record Avif(
		Avif.Compression Mode,
		[Range(0, 100)] int Quality,
		[Range(8, 12)] int? BitDepth,
		int TargetRes
	) : ImageFormat("avif", "image/avif")
	{
		public enum Compression
		{
			Lossy,
			Lossless
		}
	}

	public record Jxl(
		Jxl.Compression Mode,
		[Range(0, 100)] int Quality,
		[Range(1, 9)] int Effort,
		int TargetRes
	) : ImageFormat("jxl", "image/jxl")
	{
		public enum Compression
		{
			Lossy,
			Lossless
		}
	}
}

public enum ImageFormatEnum
{
	None,
	Keep,
	Webp,
	Avif,
	Jxl
}

public record ImageVersion(ImageVersion.KeyEnum Key, ImageFormat Format)
{
	public enum KeyEnum
	{
		Original,
		Thumbnail,
		Public
	}

	public static ImageVersion Stub => new(KeyEnum.Original, new ImageFormat.Keep("", ""));
}