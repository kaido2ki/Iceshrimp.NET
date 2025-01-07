using Microsoft.AspNetCore.Components;

namespace Iceshrimp.Backend.Components.PublicPreview.Schemas;

public class PreviewNote
{
	public required PreviewUser              User;
	public required string?                  RawText;
	public required MarkupString?            Text;
	public required string?                  Cw;
	public required string?                  QuoteUrl;
	public required bool                     QuoteInaccessible;
	public required List<PreviewAttachment>? Attachments;
	public required PreviewPoll?             Poll;
	public required string                   CreatedAt;
	public required string?                  UpdatedAt;
}

public class PreviewAttachment
{
	public required string  MimeType;
	public required string  Url;
	public required string  Name;
	public required string? Alt;
	public required bool    Sensitive;
}

public class PreviewPoll
{
	public required DateTime?                       ExpiresAt   { get; set; }
	public required bool                            Multiple    { get; set; }
	public required List<(string Value, int Votes)> Choices     { get; set; }
	public required int?                            VotersCount { get; set; }
}