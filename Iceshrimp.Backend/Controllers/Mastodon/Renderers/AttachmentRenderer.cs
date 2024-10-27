using Iceshrimp.Backend.Controllers.Mastodon.Schemas.Entities;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Services;

namespace Iceshrimp.Backend.Controllers.Mastodon.Renderers;

public class AttachmentRenderer(MediaProxyService mediaProxy) : ISingletonService
{
	public AttachmentEntity Render(DriveFile file, bool proxy = true) => new()
	{
		Id          = file.Id,
		Type        = AttachmentEntity.GetType(file.Type),
		Url         = proxy ? mediaProxy.GetProxyUrl(file) : file.RawAccessUrl,
		Blurhash    = file.Blurhash,
		PreviewUrl  = proxy ? mediaProxy.GetThumbnailProxyUrl(file) : file.RawThumbnailAccessUrl,
		Description = file.Comment,
		RemoteUrl   = file.Uri,
		Sensitive   = file.IsSensitive,
		//
		Metadata = file.Properties is { Height: { } height, Width: { } width }
			? new AttachmentMetadata(width, height)
			: null
	};
}
