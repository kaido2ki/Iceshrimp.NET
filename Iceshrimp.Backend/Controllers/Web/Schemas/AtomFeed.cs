using System.Xml.Serialization;

namespace Iceshrimp.Backend.Controllers.Web.Schemas;

public abstract class AtomCommonAttributes
{
    [XmlAttribute("base")] public string? Base { get; set; }
    [XmlAttribute("lang")] public string? Lang { get; set; }
}

public class AtomPlainText : AtomCommonAttributes
{
    [XmlText]              public required string Text { get; set; }
    [XmlAttribute("type")] public          string Type { get; set; } = "text";
}

public class AtomHtmlText : AtomPlainText
{
    public new string Type => "html";
}

public class AtomPerson : AtomCommonAttributes
{
    [XmlElement("name")]  public required string  Name  { get; set; }
    [XmlElement("uri")]   public          string? Uri   { get; set; }
    [XmlElement("email")] public          string? Email { get; set; }
}

public class AtomCategory : AtomCommonAttributes
{
    [XmlAttribute("term")]   public required string  Term   { get; set; }
    [XmlAttribute("scheme")] public          string? Scheme { get; set; }
    [XmlAttribute("label")]  public          string? Label  { get; set; }
}

public class AtomContributor : AtomPerson;

public class AtomGenerator : AtomCommonAttributes
{
    [XmlText]                 public required string  Text    { get; set; }
    [XmlAttribute("uri")]     public          string? Uri     { get; set; }
    [XmlAttribute("version")] public          string? Version { get; set; }
}

public class AtomIcon : AtomCommonAttributes
{
    [XmlText] public required string Uri { get; set; }
}

public class AtomId : AtomCommonAttributes
{
    [XmlText] public required string Uri { get; set; }
}

public class AtomLink : AtomCommonAttributes
{
    [XmlAttribute("href")]     public required string  Href     { get; set; }
    [XmlAttribute("rel")]      public          string? Rel      { get; set; }
    [XmlAttribute("type")]     public          string? Type     { get; set; }
    [XmlAttribute("hreflang")] public          string? HrefLang { get; set; }
    [XmlAttribute("title")]    public          string? Title    { get; set; }
    [XmlAttribute("length")]   public          string? Length   { get; set; }
}

public class AtomLogo : AtomCommonAttributes
{
    [XmlText] public required string Uri { get; set; }
}

public class AtomInlineTextContent
{
    [XmlAttribute("type")] public          string? Type { get; set; } = "text";
    [XmlText]              public required string  Text { get; set; }
}

public class AtomInlineOtherContent
{
    [XmlAttribute("type")] public          string? MediaType { get; set; }
    [XmlArray]             public required object  Content   { get; set; }
}

public class AtomOutOfLineContent
{
    [XmlAttribute("type")] public          string? MediaType { get; set; }
    [XmlAttribute("src")]  public required string  Src       { get; set; }
}

public class AtomSource : AtomCommonAttributes
{
    [XmlElement("author")]      public List<AtomPerson>?      Authors      { get; set; }
    [XmlElement("category")]    public List<AtomCategory>?    Categories   { get; set; }
    [XmlElement("contributor")] public List<AtomContributor>? Contributors { get; set; }
    [XmlElement("generator")]   public AtomGenerator?         Generator    { get; set; }
    [XmlElement("icon")]        public AtomIcon?              Icon         { get; set; }
    [XmlElement("id")]          public AtomId?                Id           { get; set; }
    [XmlElement("link")]        public List<AtomLink>?        Links        { get; set; }
    [XmlElement("logo")]        public AtomLogo?              Logo         { get; set; }
    [XmlElement("rights")]      public AtomPlainText?         Rights       { get; set; }
    [XmlElement("subtitle")]    public AtomPlainText?         Subtitle     { get; set; }
    [XmlElement("title")]       public AtomPlainText?         Title        { get; set; }
    [XmlElement("updated")]     public DateTime?              UpdatedAt    { get; set; }
}

public class AtomEntry : AtomCommonAttributes
{
    [XmlIgnore]                 public          string                 RawId        { get; set; } = "";
    [XmlElement("author")]      public          List<AtomPerson>?      Authors      { get; set; }
    [XmlElement("category")]    public          List<AtomCategory>?    Categories   { get; set; }
    [XmlElement("content")]     public          AtomInlineTextContent? Content      { get; set; }
    [XmlElement("contributor")] public          List<AtomContributor>? Contributors { get; set; }
    [XmlElement("id")]          public required AtomId                 Id           { get; set; }
    [XmlElement("link")]        public          List<AtomLink>?        Links        { get; set; }
    [XmlElement("published")]   public          DateTime?              PublishedAt  { get; set; }
    [XmlElement("rights")]      public          AtomPlainText?         Rights       { get; set; }
    [XmlElement("source")]      public          AtomSource?            Source       { get; set; }
    [XmlElement("summary")]     public          AtomPlainText?         Summary      { get; set; }
    [XmlElement("title")]       public required AtomPlainText          Title        { get; set; }
    [XmlElement("updated")]     public required DateTime               UpdatedAt    { get; set; }
}

[XmlRoot("feed", Namespace = "http://www.w3.org/2005/Atom")]
public sealed class AtomFeed : AtomCommonAttributes
{
    [XmlElement("author")]      public required List<AtomPerson>       Authors      { get; set; }
    [XmlElement("category")]    public          List<AtomCategory>?    Categories   { get; set; }
    [XmlElement("contributor")] public          List<AtomContributor>? Contributors { get; set; }
    [XmlElement("generator")]   public          AtomGenerator?         Generator    { get; set; }
    [XmlElement("icon")]        public          AtomIcon?              Icon         { get; set; }
    [XmlElement("id")]          public required AtomId                 Id           { get; set; }
    [XmlElement("link")]        public          List<AtomLink>?        Links        { get; set; }
    [XmlElement("logo")]        public          AtomLogo?              Logo         { get; set; }
    [XmlElement("rights")]      public          AtomPlainText?         Rights       { get; set; }
    [XmlElement("subtitle")]    public          AtomPlainText?         Subtitle     { get; set; }
    [XmlElement("title")]       public required AtomPlainText          Title        { get; set; }
    [XmlElement("updated")]     public required DateTime               UpdatedAt    { get; set; }
    [XmlElement("entry")]       public required List<AtomEntry>        Entries      { get; set; }
}
