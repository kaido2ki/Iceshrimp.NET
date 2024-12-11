using Iceshrimp.Backend.Controllers.Mastodon.Schemas.Entities;
using Iceshrimp.Backend.Core.Database.Tables;

namespace Iceshrimp.Backend.Controllers.Mastodon.Renderers;

public static class AttachmentRenderer
{
	public static AttachmentEntity Render(DriveFile file) => new()
	{
		Id          = file.Id,
		Type        = AttachmentEntity.GetType(file.Type),
		Url         = file.AccessUrl,
		Blurhash    = file.Blurhash,
		PreviewUrl  = file.ThumbnailAccessUrl,
		Description = file.Comment,
		RemoteUrl   = file.Uri,
		Sensitive   = file.IsSensitive,
		//
		Metadata = file.Properties is { Height: { } height, Width: { } width }
			? new AttachmentMetadata(width, height)
			: null
	};
}
