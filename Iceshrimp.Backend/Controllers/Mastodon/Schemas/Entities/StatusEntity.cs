using System.Text.Json.Serialization;
using Iceshrimp.Backend.Controllers.Pleroma.Schemas.Entities;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Shared.Helpers;
using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;
using JI = System.Text.Json.Serialization.JsonIgnoreAttribute;

namespace Iceshrimp.Backend.Controllers.Mastodon.Schemas.Entities;

public interface IPostNotePayload {}

public class StatusEntity : IIdentifiable, ICloneable, IPostNotePayload
{
	[JI]                          public          string?        MastoReplyUserId;
	[J("text")]                   public required string?        Text           { get; set; }
	[J("content")]                public required string?        Content        { get; set; }
	[J("uri")]                    public required string         Uri            { get; set; }
	[J("url")]                    public required string?        Url            { get; set; }
	[J("account")]                public required AccountEntity  Account        { get; set; }
	[J("in_reply_to_id")]         public required string?        ReplyId        { get; set; }
	[J("in_reply_to_account_id")] public required string?        ReplyUserId    { get; set; }
	[J("reblog")]                 public required StatusEntity?  Renote         { get; set; }
	[J("quote")]                  public required StatusEntity?  Quote          { get; set; }
	[J("quote_id")]               public required string?        QuoteId        { get; set; }
	[J("quote_approval")]         public required QuoteApproval? QuoteApproval  { get; set; }
	[J("content_type")]           public required string         ContentType    { get; set; }
	[J("created_at")]             public required string         CreatedAt      { get; set; }
	[J("edited_at")]              public required string?        EditedAt       { get; set; }
	[J("replies_count")]          public required long           RepliesCount   { get; set; }
	[J("reblogs_count")]          public required long           RenoteCount    { get; set; }
	[J("favourites_count")]       public required long           FavoriteCount  { get; set; }
	[J("reactions_count")]        public required long           ReactionsCount { get; set; }
	[J("reblogged")]              public required bool?          IsRenoted      { get; set; }
	[J("favourited")]             public required bool?          IsFavorited    { get; set; }
	[J("bookmarked")]             public required bool?          IsBookmarked   { get; set; }
	[J("muted")]                  public required bool?          IsMuted        { get; set; }
	[J("sensitive")]              public required bool           IsSensitive    { get; set; }
	[J("spoiler_text")]           public required string         ContentWarning { get; set; }
	[J("visibility")]             public required string         Visibility     { get; set; }

	[J("pinned")]
	[JI(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public required bool? IsPinned { get; set; }

	[J("poll")] public required PollEntity? Poll { get; set; }

	[J("filtered")]          public required List<FilterResultEntity> Filtered    { get; set; }
	[J("mentions")]          public required List<MentionEntity>      Mentions    { get; set; }
	[J("media_attachments")] public required List<AttachmentEntity>   Attachments { get; set; }
	[J("emojis")]            public required List<EmojiEntity>        Emojis      { get; set; }
	[J("reactions")]         public required List<ReactionEntity>     Reactions   { get; set; }
	[J("tags")]              public required List<StatusTags>         Tags        { get; set; }

	[J("card")]        public object?      Card        => null; //FIXME
	[J("application")] public object?      Application => null; //FIXME

	[J("language")] public string? Language => null; //FIXME

	public                    object Clone() => MemberwiseClone();
	[J("id")] public required string Id      { get; set; }

	[J("pleroma")] [JI(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public required PleromaStatusExtensions? Pleroma { get; set; }
	
	// HACK: make Status also a valid Quote entity for client compatibility
	// https://issues.iceshrimp.dev/issue/ISH-871#comment-019c24ed-c841-7de2-9c69-85e2951135ca
	[J("state")] [JI(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public QuoteState? State { get; set; }
	
	[J("quoted_status")] [JI(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public StatusEntity? QuotedStatus { get; set; }

	public static string EncodeVisibility(Note.NoteVisibility visibility)
	{
		return visibility switch
		{
			Note.NoteVisibility.Public    => "public",
			Note.NoteVisibility.Home      => "unlisted",
			Note.NoteVisibility.Followers => "private",
			Note.NoteVisibility.Specified => "direct",
			_                             => throw new GracefulException($"Unknown visibility: {visibility}")
		};
	}

	public static Note.NoteVisibility DecodeVisibility(string visibility)
	{
		return visibility switch
		{
			"public"   => Note.NoteVisibility.Public,
			"unlisted" => Note.NoteVisibility.Home,
			"private"  => Note.NoteVisibility.Followers,
			"direct"   => Note.NoteVisibility.Specified,
			_          => throw GracefulException.BadRequest($"Unknown visibility: {visibility}")
		};
	}
}

public class StatusContext
{
	[J("ancestors")]   public required List<StatusEntity> Ancestors   { get; set; }
	[J("descendants")] public required List<StatusEntity> Descendants { get; set; }
}

public class StatusTranslation
{
	[J("content")]                  public required string?       Content                { get; set; }
	[J("spoiler_text")]             public required string?       ContentWarning         { get; set; }
	[J("language")]                 public required string        Language               { get; set; }
	[J("detected_source_language")] public required string        DetectedSourceLanguage { get; set; }
	[J("poll")]                     public required object?       Poll                   { get; set; }
	[J("provider")]                 public required string        Provider               { get; set; }
	[J("media_attachments")]        public          List<object>? MediaAttachments       { get; set; } = []; //TODO
}

public class StatusSource
{
	[J("id")]           public required string Id             { get; set; }
	[J("text")]         public required string Text           { get; set; }
	[J("spoiler_text")] public required string ContentWarning { get; set; }
}

public class StatusEdit
{
	[J("content")]           public required string?                Content        { get; set; }
	[J("spoiler_text")]      public required string                 ContentWarning { get; set; }
	[J("sensitive")]         public required bool                   IsSensitive    { get; set; }
	[J("created_at")]        public required string                 CreatedAt      { get; set; }
	[J("account")]           public required AccountEntity          Account        { get; set; }
	[J("poll")]              public required PollEntity?            Poll           { get; set; }
	[J("media_attachments")] public required List<AttachmentEntity> Attachments    { get; set; }
	[J("emojis")]            public required List<EmojiEntity>      Emojis         { get; set; }
}

public class StatusTags
{
	[J("name")] public required string Name { get; set; }
	[J("url")]  public required string Url  { get; set; }
}

public enum QuoteState
{
	Pending,
	Accepted,
	Rejected,
	Revoked,
	Deleted,
	Unauthorized,
}

public class QuoteApproval
{
	[J("automatic")]    public required QuoteAuthorization[]          Automatic   { get; set; }
	[J("manual")]       public required QuoteAuthorization[]          Manual      { get; set; }
	[J("current_user")] public required CurrentUserQuoteAuthorization CurrentUser { get; set; }
}

public enum QuoteAuthorization
{
	UnsupportedPolicy,
	Public,
	Followers,
	Following
}

public enum CurrentUserQuoteAuthorization
{
	Unknown,
	Automatic,
	Manual,
	Denied
}

public class ScheduledStatusEntity : IIdentifiable, IPostNotePayload
{
    [J("id")]                public required string                 Id          { get; set; }
    [J("scheduled_at")]      public required DateTime               ScheduledAt { get; set; }
    [J("params")]            public required Param                  Params      { get; set; }
    [J("media_attachments")] public required List<AttachmentEntity> Attachments { get; set; }

    public class Param
    {
        [J("text")]           public string?            Text        { get; set; }
        [J("in_reply_to_id")] public string?            ReplyId     { get; set; }
        [J("sensitive")]      public bool               Sensitive   { get; set; } = false;
        [J("spoiler_text")]   public string?            Cw          { get; set; }
        [J("visibility")]     public string             Visibility  { get; set; } = null!;
        [J("language")]       public string?            Language    { get; set; }
        [J("scheduled_at")]   public string?            ScheduledAt { get; set; }
        [J("media_ids")]      public List<string>?      MediaIds    { get; set; }
        [J("local_only")]     public bool               LocalOnly   { get; set; } = false;
        [J("quote_id")]       public string?            QuoteId     { get; set; }
        [J("reblog_id")]      public string?            ReblogId    { get; set; }
        [J("poll")]           public ScheduledPollData? Poll        { get; set; }

        public class ScheduledPollData
        {
            [J("options")]     public List<string> Options    { get; set; } = null!;
            [J("expires_in")]  public long         ExpiresIn  { get; set; }
            [J("multiple")]    public bool         Multiple   { get; set; } = false;
        }
    }
}

