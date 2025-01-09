using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Core.Services;

[UsedImplicitly]
public class MediaProxyService(IOptions<Config.InstanceSection> instance) : ISingletonService
{
	private string GetProxyUrl(DriveFile file, bool thumbnail)
	{
		var url = thumbnail ? file.RawThumbnailAccessUrl : file.RawAccessUrl;
		if (file.UserHost is null || !file.IsLink)
			return url;

		return GetProxyUrlInternal($"files/{file.AccessKey}", thumbnail && file.ThumbnailUrl != null);
	}

	public string GetProxyUrl(Emoji emoji) => emoji.Host is null
		? emoji.RawPublicUrl
		: GetProxyUrlInternal($"emoji/{emoji.Id}", thumbnail: false);

	public string GetProxyUrl(DriveFile file)          => GetProxyUrl(file, thumbnail: false);
	public string GetThumbnailProxyUrl(DriveFile file) => GetProxyUrl(file, thumbnail: true);

	private string GetProxyUrlInternal(string route, bool thumbnail) => thumbnail
		? $"https://{instance.Value.WebDomain}/{route}/thumbnail"
		: $"https://{instance.Value.WebDomain}/{route}";
}
