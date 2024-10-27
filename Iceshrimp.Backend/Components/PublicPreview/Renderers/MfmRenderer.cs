using Iceshrimp.Backend.Components.PublicPreview.Schemas;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Helpers.LibMfm.Conversion;
using Iceshrimp.MfmSharp;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Components;

namespace Iceshrimp.Backend.Components.PublicPreview.Renderers;

public readonly record struct MfmRenderData(MarkupString Html, List<MfmInlineMedia> InlineMedia);

[UsedImplicitly]
public class MfmRenderer(MfmConverter converter) : ISingletonService
{
	public async Task<MfmRenderData?> RenderAsync(
		string? text, string? host, List<Note.MentionedUser> mentions, List<Emoji> emoji, string rootElement,
		List<PreviewAttachment>? media = null
	)
	{
		if (text is null) return null;
		var parsed     = MfmParser.Parse(text);

		// Ensure we are rendering HTML markup (AsyncLocal)
		converter.SupportsHtmlFormatting.Value = true;
		converter.SupportsInlineMedia.Value = true;

		var mfmInlineMedia = media?.Select(m => new MfmInlineMedia(MfmInlineMedia.GetType(m.MimeType), m.Url, m.Alt)).ToList();
		var serialized = await converter.ToHtmlAsync(parsed, mentions, host, emoji: emoji, rootElement: rootElement, media: mfmInlineMedia);

		return new MfmRenderData(new MarkupString(serialized.Html), serialized.InlineMedia);
	}

	public async Task<MarkupString?> RenderSimpleAsync(string? text, string? host, List<Note.MentionedUser> mentions, List<Emoji> emoji, string rootElement)
	{
		var rendered = await RenderAsync(text, host, mentions, emoji, rootElement);
		return rendered?.Html;
	}
}
