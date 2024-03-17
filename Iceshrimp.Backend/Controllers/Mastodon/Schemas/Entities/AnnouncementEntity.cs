using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace Iceshrimp.Backend.Controllers.Mastodon.Schemas.Entities;

public class AnnouncementEntity
{
	[J("reactions")] public List<object> Reactions = []; //FIXME

	[J("statuses")]     public          List<object>        Statuses = []; //FIXME
	[J("tags")]         public          List<object>        Tags     = []; //FIXME
	[J("id")]           public required string              Id          { get; set; }
	[J("content")]      public required string              Content     { get; set; }
	[J("published_at")] public required string              PublishedAt { get; set; }
	[J("updated_at")]   public required string              UpdatedAt   { get; set; }
	[J("read")]         public required bool                IsRead      { get; set; }
	[J("mentions")]     public required List<MentionEntity> Mentions    { get; set; }
	[J("emojis")]       public required List<EmojiEntity>   Emoji       { get; set; }

	[J("published")] public bool Published => true;
	[J("all_day")]   public bool AllDay    => false;
}