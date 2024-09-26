using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Helpers.LibMfm.Conversion;
using Iceshrimp.Parsing;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Components.PublicPreview.Renderers;

public class MfmRenderer(IOptions<Config.InstanceSection> config)
{
	private readonly MfmConverter _converter = new(config);

	public async Task<MarkupString?> Render(
		string? text, string? host, List<Note.MentionedUser> mentions, List<Emoji> emoji, string rootElement
	)
	{
		if (text is null) return null;
		var parsed     = Mfm.parse(text);
		var serialized = await _converter.ToHtmlAsync(parsed, mentions, host, emoji: emoji, rootElement: rootElement);
		return new MarkupString(serialized);
	}
}