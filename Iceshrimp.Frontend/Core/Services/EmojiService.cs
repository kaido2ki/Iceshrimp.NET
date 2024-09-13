using Iceshrimp.Frontend.Core.Miscellaneous;
using Iceshrimp.Shared.Schemas.Web;
using Microsoft.AspNetCore.Components;

namespace Iceshrimp.Frontend.Core.Services;

internal class EmojiService(ApiService api)
{
	[Inject] private ApiService           Api    { get; set; } = api;
	private          List<EmojiResponse>? Emojis { get; set; }

	public async Task<List<EmojiResponse>> GetEmoji()
	{
		if (Emojis is not null) return Emojis;
		try
		{
			var emoji = await Api.Emoji.GetAllEmoji();
			Emojis = emoji;
			return Emojis;
		}
		catch (ApiException)
		{
			// FIXME: Implement connection error handling
			throw new Exception("Failed to fetch emoji");
		}
	}
}