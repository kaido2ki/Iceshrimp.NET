using Iceshrimp.Backend.Components.PublicPreview.Schemas;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Helpers.LibMfm.Conversion;
using Iceshrimp.Backend.Core.Services;
using Iceshrimp.MfmSharp;
using Iceshrimp.Utils.DependencyInjection;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Components;

namespace Iceshrimp.Backend.Components.PublicPreview.Renderers;

public readonly record struct MfmRenderData(MarkupString Html, List<MfmInlineMedia> InlineMedia);

[UsedImplicitly]
public class MfmRenderer(MfmConverter converter, FlagService flags) : ISingletonService
{
	public MfmRenderData? Render(
		string? text, string? host, List<Note.MentionedUser> mentions, List<Emoji> emoji, string rootElement,
		List<PreviewAttachment>? media = null
	)
	{
		if (text is null) return null;
		var parsed     = MfmParser.Parse(text);

		// Ensure we are rendering HTML markup (AsyncLocal)
		flags.SupportsHtmlFormatting.Value = true;
		flags.SupportsInlineMedia.Value = true;

		var mfmInlineMedia = media?.Select(m => new MfmInlineMedia(MfmInlineMedia.GetType(m.MimeType), m.Url, m.Alt)).ToList();
		var serialized = converter.ToHtml(parsed, mentions, host, emoji: emoji, rootElement: rootElement, media: mfmInlineMedia);

		return new MfmRenderData(new MarkupString(serialized.Html), serialized.InlineMedia);
	}

	public MarkupString? RenderSimple(string? text, string? host, List<Note.MentionedUser> mentions, List<Emoji> emoji, string rootElement)
	{
		var rendered = Render(text, host, mentions, emoji, rootElement);
		return rendered?.Html;
	}
}
