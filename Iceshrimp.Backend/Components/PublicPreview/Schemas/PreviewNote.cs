using Microsoft.AspNetCore.Components;

namespace Iceshrimp.Backend.Components.PublicPreview.Schemas;

public class PreviewNote
{
	public required string                   Id;
	public required PreviewUser              User;
	public required string?                  RawText;
	public required MarkupString?            Text;
	public required string?                  Cw;
	public required string?                  Uri;
	public required string?                  QuoteUrl;
	public required bool                     QuoteInaccessible;
	public required List<PreviewAttachment>? Attachments;
	public required PreviewPoll?             Poll;
	public required DateTime                 CreatedAt;
	public required DateTime?                UpdatedAt;
	public required int                      RepliesCount;
	public required int                      RenoteCount;
	public required int                      LikeCount;
	public required List<PreviewReaction>    Reactions;
	public required PreviewNote?             Reply;
	public required string?                  ReplyId;
	public          PreviewNote?             Parent;
	public          List<PreviewNote>?       Descendants;
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

public class PreviewReaction
{
	public required string  NoteId    { get; set; }
	public required string  Name      { get; set; }
	public required int     Count     { get; set; }
	public required string? Url       { get; set; }
	public required bool    Sensitive { get; set; }
}