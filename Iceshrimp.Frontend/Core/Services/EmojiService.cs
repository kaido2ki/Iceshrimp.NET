using System.Text.Json;
using Iceshrimp.Frontend.Components;
using Iceshrimp.Frontend.Core.Miscellaneous;
using Iceshrimp.Shared.Schemas.Web;
using Microsoft.AspNetCore.Components;

namespace Iceshrimp.Frontend.Core.Services;

internal class EmojiService(ApiService api, GlobalComponentSvc global)
{
	[Inject] private ApiService           Api    { get; set; } = api;
	[Inject] private GlobalComponentSvc   Global { get; set; } = global;
	
	/// <summary>
	/// List of custom emojis for the instance. Call <see cref="GetEmojiAsync"/> before getting this.
	/// </summary>
	public List<EmojiResponse>? Emojis { get; set; }

	/// <summary>
	/// Initialize the <see cref="Emojis"/> list for the session. If the list is already populated this function is a no-op. It is safe to call this multiple times per session.
	/// </summary>
	public async Task GetEmojiAsync()
	{
		if (Emojis != null) return;
		try
		{
			Emojis = await Api.Emoji.GetAllEmojiAsync();
		}
		catch (ApiException e)
		{
			await Global.NoticeDialog?.DisplayApiError(e)!;
			Emojis = [];
		}
	}
}