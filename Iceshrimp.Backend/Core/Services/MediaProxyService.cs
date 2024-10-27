using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;
using static Iceshrimp.Backend.Core.Configuration.Enums;

namespace Iceshrimp.Backend.Core.Services;

[UsedImplicitly]
public class MediaProxyService(
	IOptions<Config.InstanceSection> instance,
	IOptionsMonitor<Config.StorageSection> storage
) : ISingletonService
{
	public string? GetProxyUrl(string? url, string? accessKey, bool thumbnail = false, string route = "files")
	{
		if (!storage.CurrentValue.ProxyRemoteMedia || url is null || accessKey is null) return url;

		// Don't proxy local / object storage urls
		if (
			storage.CurrentValue.Provider is FileStorage.ObjectStorage
			&& storage.CurrentValue.ObjectStorage?.AccessUrl is { } accessUrl
			&& (url.StartsWith(accessUrl) || url.StartsWith($"https://{instance.Value.WebDomain}/"))
		)
		{
			return url;
		}

		return GetProxyUrlInternal($"{route}/{accessKey}", thumbnail);
	}

	public string GetProxyUrl(DriveFile file, bool thumbnail)
	{
		var url = thumbnail ? file.RawThumbnailAccessUrl : file.RawAccessUrl;
		if (file.UserHost is null || !file.IsLink || file.AccessKey == null)
			return url;

		return GetProxyUrlInternal($"files/{file.AccessKey}", thumbnail);
	}

	public string GetProxyUrl(Emoji emoji) => emoji.Host is null
		? emoji.PublicUrl
		: GetProxyUrlInternal($"emoji/{emoji.Id}", thumbnail: false);

	public string GetProxyUrl(DriveFile file)          => GetProxyUrl(file, thumbnail: false);
	public string GetThumbnailProxyUrl(DriveFile file) => GetProxyUrl(file, thumbnail: true);

	public string? GetAvatarProxyUrl(User user, bool thumbnail = true)
		=> user.IsLocalUser
			? user.AvatarUrl
			: GetProxyUrl(user.AvatarUrl, user.Id, thumbnail, route: "avatars");

	public string? GetBannerProxyUrl(User user, bool thumbnail = true)
		=> user.IsLocalUser
			? user.BannerUrl
			: GetProxyUrl(user.BannerUrl, user.Id, thumbnail, route: "banners");

	private string GetProxyUrlInternal(string route, bool thumbnail) => thumbnail
		? $"https://{instance.Value.WebDomain}/{route}/thumbnail"
		: $"https://{instance.Value.WebDomain}/{route}";
}
