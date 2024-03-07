using System.Text.Json.Serialization;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Middleware;
using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;
using JI = System.Text.Json.Serialization.JsonIgnoreAttribute;

namespace Iceshrimp.Backend.Controllers.Mastodon.Schemas.Entities;

public class StatusEntity : IEntity
{
	[J("id")]                     public required string        Id             { get; set; }
	[J("content")]                public required string?       Content        { get; set; }
	[J("uri")]                    public required string        Uri            { get; set; }
	[J("url")]                    public required string        Url            { get; set; }
	[J("account")]                public required AccountEntity Account        { get; set; }
	[J("in_reply_to_id")]         public required string?       ReplyId        { get; set; }
	[J("in_reply_to_account_id")] public required string?       ReplyUserId    { get; set; }
	[J("reblog")]                 public required StatusEntity? Renote         { get; set; }
	[J("quote")]                  public required StatusEntity? Quote          { get; set; }
	[J("content_type")]           public required string        ContentType    { get; set; }
	[J("created_at")]             public required string        CreatedAt      { get; set; }
	[J("edited_at")]              public required string?       EditedAt       { get; set; }
	[J("replies_count")]          public required long          RepliesCount   { get; set; }
	[J("reblogs_count")]          public required long          RenoteCount    { get; set; }
	[J("favourites_count")]       public required long          FavoriteCount  { get; set; }
	[J("reblogged")]              public required bool?         IsRenoted      { get; set; }
	[J("favourited")]             public required bool?         IsFavorited    { get; set; }
	[J("bookmarked")]             public required bool?         IsBookmarked   { get; set; }
	[J("muted")]                  public required bool?         IsMuted        { get; set; }
	[J("sensitive")]              public required bool          IsSensitive    { get; set; }
	[J("spoiler_text")]           public required string        ContentWarning { get; set; }
	[J("visibility")]             public required string        Visibility     { get; set; }

	[J("text")]
	[JI(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public required string? Text { get; set; }

	[J("pinned")]
	[JI(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public required bool? IsPinned { get; set; }

	[J("poll")] public required PollEntity? Poll { get; set; }

	[J("mentions")]          public required List<MentionEntity>    Mentions    { get; set; }
	[J("media_attachments")] public required List<AttachmentEntity> Attachments { get; set; }
	[J("emojis")]            public required List<EmojiEntity>      Emojis      { get; set; }
	[J("reactions")]         public required List<ReactionEntity>   Reactions   { get; set; }

	[J("tags")]        public object[] Tags        => [];   //FIXME
	[J("filtered")]    public object[] Filtered    => [];   //FIXME
	[J("card")]        public object?  Card        => null; //FIXME
	[J("application")] public object?  Application => null; //FIXME

	[J("language")] public string? Language => null; //FIXME

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

public class StatusSource
{
	[J("id")]           public required string Id             { get; set; }
	[J("text")]         public required string Text           { get; set; }
	[J("spoiler_text")] public required string ContentWarning { get; set; }
}