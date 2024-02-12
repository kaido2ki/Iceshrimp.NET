using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace Iceshrimp.Backend.Controllers.Mastodon.Schemas.Entities;

public class Attachment {
	[J("id")]          public required string              Id          { get; set; }
	[J("url")]         public required string              Url         { get; set; }
	[J("remote_url")]  public          string?             RemoteUrl   { get; set; }
	[J("preview_url")] public          string?             PreviewUrl  { get; set; }
	[J("text_url")]    public          string?             TextUrl     { get; set; }
	[J("meta")]        public          AttachmentMetadata? Metadata    { get; set; }
	[J("description")] public          string?             Description { get; set; }
	[J("blurhash")]    public          string?             Blurhash    { get; set; }

	public required AttachmentType Type;

	[J("type")]
	public string TypeString => Type switch {
		AttachmentType.Unknown => "unknown",
		AttachmentType.Image   => "image",
		AttachmentType.Gif     => "gifv",
		AttachmentType.Video   => "video",
		AttachmentType.Audio   => "audio",
		_                      => throw new ArgumentOutOfRangeException()
	};

	public static AttachmentType GetType(string mime) {
		if (mime == "image/gif") return AttachmentType.Gif;
		if (mime.StartsWith("image/")) return AttachmentType.Image;
		if (mime.StartsWith("video/")) return AttachmentType.Video;
		if (mime.StartsWith("audio/")) return AttachmentType.Audio;

		return AttachmentType.Unknown;
	}
}

public enum AttachmentType {
	Unknown,
	Image,
	Gif,
	Video,
	Audio
}

public class AttachmentMetadata {
	//TODO
}