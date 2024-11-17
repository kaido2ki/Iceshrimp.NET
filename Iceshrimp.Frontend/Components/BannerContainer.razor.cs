using Iceshrimp.Frontend.Core.Services;
using Microsoft.AspNetCore.Components;

namespace Iceshrimp.Frontend.Components;

public partial class BannerContainer : ComponentBase
{
	[Inject] private GlobalComponentSvc GlobalComponentSvc { get; set; } = null!;
	private          List<Banner>       CurrentBanners     { get; set; } = [];
	private          List<Banner>       Banners            { get; }      = [];

	public void AddBanner(Banner newBanner)
	{
		Banners.Add(newBanner);
		FillBanners();
	}

	private void FillBanners()
	{
		if (Banners.Count > 0)
		{
			while (CurrentBanners.Count < 5 && Banners.Count > 0)
			{
				CurrentBanners.Add(Banners.First());
				Banners.Remove(Banners.First());
			}
		}

		StateHasChanged();
	}

	private void Close(Banner banner)
	{
		banner.OnClose?.Invoke();
		CurrentBanners.Remove(banner);
		FillBanners();
	}

	protected override void OnInitialized()
	{
		GlobalComponentSvc.BannerComponent = this;
	}

	public class Banner
	{
		public required string  Text    { get; set; }
		public Action? OnClose { get; set; }
		public Action? OnTap   { get; set; }
	}
}