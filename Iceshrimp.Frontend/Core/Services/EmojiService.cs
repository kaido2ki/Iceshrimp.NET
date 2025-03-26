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
	private          List<EmojiResponse>? Emojis { get; set; }

	public async Task<List<EmojiResponse>> GetEmojiAsync()
	{
		if (Emojis is not null) return Emojis;
		try
		{
			var emoji = await Api.Emoji.GetAllEmojiAsync();
			Emojis = emoji;
			return Emojis;
		}
		catch (ApiException e)
		{
			await Global.NoticeDialog?.Display(e.Response.Message ?? "Unable to load emojis",
			                                   NoticeDialog.NoticeType.Error)!;
			return [];
		}
		catch (JsonException)
		{
			await Global.NoticeDialog?.Display("Emojis provided by the server didn't match what the client expected",
			                                   NoticeDialog.NoticeType.Error)!;
			return [];
		}
	}
}