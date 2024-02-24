namespace Iceshrimp.Backend.Core.Configuration;

public static class Constants
{
	public const           string   ActivityStreamsNs = "https://www.w3.org/ns/activitystreams";
	public const           string   W3IdSecurityNs    = "https://w3id.org/security";
	public const           string   PurlDcNs          = "http://purl.org/dc/terms";
	public const           string   XsdNs             = "http://www.w3.org/2001/XMLSchema";
	public const           string   MastodonNs           = "http://joinmastodon.org/ns";
	public static readonly string[] SystemUsers       = ["instance.actor", "relay.actor"];

	public static readonly string[] BrowserSafeMimeTypes =
	[
		"image/png",
		"image/gif",
		"image/jpeg",
		"image/webp",
		"image/apng",
		"image/bmp",
		"image/tiff",
		"image/x-icon",
		"image/avif",
		"audio/opus",
		"video/ogg",
		"audio/ogg",
		"application/ogg",
		"video/quicktime",
		"video/mp4",
		"video/vnd.avi",
		"audio/mp4",
		"video/x-m4v",
		"audio/x-m4a",
		"video/3gpp",
		"video/3gpp2",
		"video/3gp2",
		"audio/3gpp",
		"audio/3gpp2",
		"audio/3gp2",
		"video/mpeg",
		"audio/mpeg",
		"video/webm",
		"audio/webm",
		"audio/aac",
		"audio/x-flac",
		"audio/flac",
		"audio/vnd.wave"
	];
}