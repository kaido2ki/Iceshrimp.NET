namespace Iceshrimp.Backend.Core.Services.ImageProcessing;

public interface IImageInfo
{
	public int     Width      { get; }
	public int     Height     { get; }
	public bool    IsAnimated { get; }
	public string? MimeType   { get; }
}