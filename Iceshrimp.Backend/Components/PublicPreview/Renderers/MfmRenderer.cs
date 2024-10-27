using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Helpers.LibMfm.Conversion;
using Iceshrimp.Parsing;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Components;

namespace Iceshrimp.Backend.Components.PublicPreview.Renderers;

[UsedImplicitly]
public class MfmRenderer(MfmConverter converter) : ISingletonService
{
	public async Task<MarkupString?> RenderAsync(
		string? text, string? host, List<Note.MentionedUser> mentions, List<Emoji> emoji, string rootElement
	)
	{
		if (text is null) return null;
		var parsed     = Mfm.parse(text);
		converter.SupportsHtmlFormatting.Value = true;
		var serialized = await converter.ToHtmlAsync(parsed, mentions, host, emoji: emoji, rootElement: rootElement);
		return new MarkupString(serialized);
	}
}
