using System.Text.Json.Serialization;
using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;
using JI = System.Text.Json.Serialization.JsonIgnoreAttribute;

namespace Iceshrimp.Backend.Controllers.Web.Schemas;

public class JsonFeed
{
    [J("version")] public          string Version => "https://jsonfeed.org/version/1.1";
    [J("title")]   public required string Title   { get; set; }

    [J("home_page_url")]
    [JI(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? HomePageUrl { get; set; }

    [J("feed_url")]
    [JI(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Uri { get; set; }

    [J("description")]
    [JI(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; set; }

    [J("next_url")]
    [JI(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? NextUrl { get; set; }

    [J("icon")]
    [JI(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? IconUrl { get; set; }

    [J("favicon")]
    [JI(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? FaviconUrl { get; set; }

    [J("authors")]
    [JI(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<JsonFeedAuthor>? Authors { get; set; }

    [J("language")]
    [JI(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Language { get; set; }

    [J("expired")]
    [JI(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool Expired { get; set; }

    [J("hubs")]
    [JI(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<JsonFeedHub>? Hubs { get; set; }

    [J("items")] public required List<JsonFeedItem> Items { get; set; }
}

public class JsonFeedAuthor
{
    [J("name")]
    [JI(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Name { get; set; }

    [J("url")]
    [JI(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Url { get; set; }

    [J("avatar")]
    [JI(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? AvatarUrl { get; set; }
}

public class JsonFeedHub
{
    [J("type")] public required string Type { get; set; }
    [J("url")]  public required string Url  { get; set; }
}

public class JsonFeedItem
{
    [JI(Condition = JsonIgnoreCondition.Always)]
    public string RawId { get; set; } = "";

    [J("id")] public required string Id { get; set; }

    [J("url")]
    [JI(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Url { get; set; }

    [J("external_url")]
    [JI(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ExternalUrl { get; set; }

    [J("title")]
    [JI(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Title { get; set; }

    [J("content_html")]
    [JI(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ContentHtml { get; set; }

    [J("content_text")]
    [JI(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ContentText { get; set; }

    [J("summary")]
    [JI(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Summary { get; set; }

    [J("image")]
    [JI(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ImageUrl { get; set; }

    [J("banner_image")]
    [JI(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? BannerImageUrl { get; set; }

    [J("date_published")]
    [JI(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTime? CreatedAt { get; set; }

    [J("date_modified")]
    [JI(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTime? UpdatedAt { get; set; }

    [J("authors")]
    [JI(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<JsonFeedAuthor>? Authors { get; set; }

    [J("tags")]
    [JI(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? Tags { get; set; }

    [J("language")]
    [JI(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Language { get; set; }

    [J("attachments")]
    [JI(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<JsonFeedAttachment>? Attachments { get; set; }
}

public class JsonFeedAttachment
{
    [J("url")]       public required string Url      { get; set; }
    [J("mime_type")] public required string MimeType { get; set; }

    [J("title")]
    [JI(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? FileName { get; set; }

    [J("size_in_bytes")]
    [JI(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? SizeBytes { get; set; }

    [J("duration_in_seconds")]
    [JI(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? DurationSeconds { get; set; }
}
