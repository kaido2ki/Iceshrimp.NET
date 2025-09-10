using System.Xml.Serialization;

namespace Iceshrimp.Backend.Controllers.Web.Schemas;

[XmlRoot("rss")]
public class RssFeed
{
    [XmlAttribute("version")]    public          string     Version { get; set; } = "2.0";
    [XmlElement("channel")]      public required RssChannel Channel { get; set; }
}

public class RssChannel
{
    [XmlElement("title")]          public required string Title { get; set; }
    [XmlElement("link")]           public required string Link { get; set; }
    [XmlElement("description")]    public required string Description { get; set; }
    [XmlElement("language")]       public string? Language { get; set; }
    [XmlElement("copyright")]      public string? Copyright { get; set; }
    [XmlElement("managingEditor")] public string? ManagingEditorEmail { get; set; }
    [XmlElement("webMaster")]      public string? WebMasterEmail { get; set; }
    [XmlElement("pubDate")]        public string? PublishedAt { get; set; }
    [XmlElement("lastBuildDate")]  public string? UpdatedAt { get; set; }
    [XmlElement("category")]       public List<RssCategory>? Categories { get; set; }
    [XmlElement("generator")]      public string? Generator { get; set; }
    [XmlElement("docs")]           public string DocsUrl { get; set; } = "https://www.rssboard.org/rss-specification";
    [XmlElement("ttl")]            public int? TimeToLive { get; set; } = 60;
    [XmlElement("image")]          public RssImage? Image { get; set; }
    [XmlElement("item")]           public required List<RssItem> Items { get; set; }
}

public class RssImage
{
    [XmlElement("url")]         public required string  Url         { get; set; }
    [XmlElement("title")]       public required string  Description { get; set; }
    [XmlElement("link")]        public required string  Link        { get; set; }
    [XmlElement("width")]       public          int?    Width       { get; set; }
    [XmlElement("height")]      public          int?    Height      { get; set; }
    [XmlElement("description")] public          string? Title       { get; set; }
}

public class RssItem
{
    [XmlElement("title")]       public required string              Title       { get; set; }
    [XmlElement("link")]        public required string              Link        { get; set; }
    [XmlElement("description")] public required string              Description { get; set; }
    [XmlElement("author")]      public          string?             AuthorEmail { get; set; }
    [XmlElement("category")]    public          List<RssCategory>?  Categories  { get; set; }
    [XmlElement("enclosure")]   public          List<RssEnclosure>? Enclosures  { get; set; }
    [XmlElement("guid")]        public          RssGuid?            Guid        { get; set; }
    [XmlElement("pubDate")]     public          string?             CreatedAt   { get; set; }
    [XmlElement("source")]      public          RssSource?          Source      { get; set; }
}

public class RssCategory
{
    [XmlAttribute("domain")] public          string? Domain { get; set; }
    [XmlText]                public required string  Text   { get; set; }
}

public class RssEnclosure
{
    [XmlAttribute("url")]    public required string Url  { get; set; }
    [XmlAttribute("length")] public required int    Size { get; set; }
    [XmlAttribute("type")]   public required string Type { get; set; }
}

public class RssGuid
{
    [XmlAttribute("isPermaLink")] public          bool   IsPermaLink { get; set; }
    [XmlText]                     public required string Guid        { get; set; }
}

public class RssSource
{
    [XmlAttribute("url")] public required string Url   { get; set; }
    [XmlText]             public required string Title { get; set; }
}
