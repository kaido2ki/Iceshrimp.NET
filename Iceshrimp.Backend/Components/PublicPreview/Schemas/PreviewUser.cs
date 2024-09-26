using Microsoft.AspNetCore.Components;

namespace Iceshrimp.Backend.Components.PublicPreview.Schemas;

public class PreviewUser
{
	public required string        Id;
	public required string?       RawDisplayName;
	public required MarkupString? DisplayName;
	public required MarkupString? Bio;
	public required string        Username;
	public required string        Host;
	public required string        Url;
	public required string        AvatarUrl;
	public required string?       BannerUrl;
	public required string?       MovedToUri;
}