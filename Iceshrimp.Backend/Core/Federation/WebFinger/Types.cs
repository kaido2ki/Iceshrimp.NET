using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using System.Xml.Serialization;
using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;
using JR = System.Text.Json.Serialization.JsonRequiredAttribute;
using JI = System.Text.Json.Serialization.JsonIgnoreAttribute;

namespace Iceshrimp.Backend.Core.Federation.WebFinger;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public class WebFingerLink
{
	[XmlAttribute("rel")] [J("rel")] [JR] public required string Rel { get; set; } = null!;

	[XmlAttribute("type")]
	[J("type")]
	[JI(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Type { get; set; }

	[XmlAttribute("href")]
	[J("href")]
	[JI(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Href { get; set; }

	[XmlAttribute("template")]
	[J("template")]
	[JI(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Template { get; set; }
}

[XmlRoot("XRD", Namespace = "http://docs.oasis-open.org/ns/xri/xrd-1.0", IsNullable = false)]
public sealed class WebFingerResponse
{
	[XmlElement("Link")] [J("links")] [JR] public required List<WebFingerLink> Links { get; set; } = null!;

	[XmlElement("Subject")]
	[J("subject")]
	[JR]
	public required string Subject { get; set; } = null!;

	[XmlElement("Alias")]
	[J("aliases")]
	[JI(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public List<string>? Aliases { get; set; }
}

[XmlRoot("XRD", Namespace = "http://docs.oasis-open.org/ns/xri/xrd-1.0", IsNullable = false)]
public class HostMetaResponse()
{
	[SetsRequiredMembers]
	public HostMetaResponse(string webDomain) : this()
	{
		Links =
		[
			new HostMetaResponseLink(webDomain, "application/jrd+json"),
			new HostMetaResponseLink(webDomain, "application/xrd+xml")
		];
	}

	[XmlElement("Link")] [J("links")] [JR] public required List<HostMetaResponseLink> Links { get; set; }
}

public sealed class HostMetaResponseLink() : WebFingerLink
{
	[SetsRequiredMembers]
	public HostMetaResponseLink(string webDomain, string type) : this()
	{
		Rel      = "lrdd";
		Type     = type;
		Template = $"https://{webDomain}/.well-known/webfinger?resource={{uri}}";
	}
}

public sealed class NodeInfoIndexResponse
{
	[J("links")] [JR] public List<WebFingerLink> Links { get; set; } = null!;
}

public class NodeInfoResponse
{
	[J("version")]           public string?           Version           { get; set; }
	[J("software")]          public NodeInfoSoftware? Software          { get; set; }
	[J("protocols")]         public List<string>?     Protocols         { get; set; }
	[J("services")]          public NodeInfoServices? Services          { get; set; }
	[J("usage")]             public NodeInfoUsage?    Usage             { get; set; }
	[J("metadata")]          public NodeInfoMetadata? Metadata          { get; set; }
	[J("openRegistrations")] public bool?             OpenRegistrations { get; set; }

	public class NodeInfoMetadata
	{
		[J("nodeName")]                   public string?       NodeName                   { get; set; }
		[J("nodeDescription")]            public string?       NodeDescription            { get; set; }
		[J("maintainer")]                 public Maintainer?   Maintainer                 { get; set; }
		[J("langs")]                      public List<object>? Languages                  { get; set; }
		[J("tosUrl")]                     public object?       TosUrl                     { get; set; }
		[J("repositoryUrl")]              public Uri?          RepositoryUrl              { get; set; }
		[J("feedbackUrl")]                public Uri?          FeedbackUrl                { get; set; }
		[J("themeColor")]                 public string?       ThemeColor                 { get; set; }
		[J("disableRegistration")]        public bool?         DisableRegistration        { get; set; }
		[J("disableLocalTimeline")]       public bool?         DisableLocalTimeline       { get; set; }
		[J("disableRecommendedTimeline")] public bool?         DisableRecommendedTimeline { get; set; }
		[J("disableGlobalTimeline")]      public bool?         DisableGlobalTimeline      { get; set; }
		[J("emailRequiredForSignup")]     public bool?         EmailRequiredForSignup     { get; set; }
		[J("postEditing")]                public bool?         PostEditing                { get; set; }
		[J("postImports")]                public bool?         PostImports                { get; set; }
		[J("enableHcaptcha")]             public bool?         EnableHCaptcha             { get; set; }
		[J("enableRecaptcha")]            public bool?         EnableRecaptcha            { get; set; }
		[J("maxNoteTextLength")]          public long?         MaxNoteTextLength          { get; set; }
		[J("maxCaptionTextLength")]       public long?         MaxCaptionTextLength       { get; set; }
		[J("enableGithubIntegration")]    public bool?         EnableGithubIntegration    { get; set; }
		[J("enableDiscordIntegration")]   public bool?         EnableDiscordIntegration   { get; set; }
		[J("enableEmail")]                public bool?         EnableEmail                { get; set; }

		[J("post_formats")]               public string[] PostFormats => ["text/plain", "text/x.misskeymarkdown"];
		[J("features")]                   public string[] Features    => ["pleroma_api", "akkoma_api", "mastodon_api", "mastodon_api_streaming", "polls", "quote_posting", "editing", "pleroma_emoji_reactions", "exposable_reactions", "custom_emoji_reactions"];
		[J("localBubbleInstances")]       public string[] LocalBubbleInstances { get; set; } = [];
		// TODO: list of ap object ids i believe?
		[J("staffAccounts")]              public string[] StaffAccounts { get; set; } = [];

		[J("publicTimelineVisibility")] public PleromaPublicTimelineVisibility? PublicTimelineVisibility { get; set; }
		[J("uploadLimits")]             public PleromaUploadLimits?             UploadLimits             { get; set; }
		[J("suggestions")]              public PleromaSuggestions?              Suggestions              { get; set; }
		[J("federation")]               public PleromaFederation?               Federation               { get; set; }
	}

	public class PleromaPublicTimelineVisibility
	{
		[J("bubble")]    public bool? Bubble    { get; set; }
		[J("federated")] public bool? Federated { get; set; }
		[J("local")]     public bool? Local     { get; set; }
	}

	public class PleromaUploadLimits
	{
		[J("general")]    public long? General    { get; set; }
		[J("avatar")]     public long? Avatar     { get; set; }
		[J("background")] public long? Background { get; set; }
		[J("banner")]     public long? Banner     { get; set; }
	}

	public class PleromaSuggestions
	{
		[J("enabled")] public bool? Enabled { get; set; }
	}

	public class PleromaFederation
	{
		[J("enabled")] public bool? Enabled { get; set; }
	}

	public class Maintainer
	{
		[J("name")]  public string? Name  { get; set; }
		[J("email")] public string? Email { get; set; }
	}

	public class NodeInfoServices
	{
		[J("inbound")]  public List<object>? Inbound  { get; set; }
		[J("outbound")] public List<string>? Outbound { get; set; }
	}

	public class NodeInfoSoftware
	{
		[J("name")]     public string? Name     { get; set; }
		[J("version")]  public string? Version  { get; set; }
		[J("codename")] public string? Codename { get; set; }
		[J("edition")]  public string? Edition  { get; set; }
		[J("homepage")] public Uri?    Homepage { get; set; }

		/// <remarks>
		///     This is only part of nodeinfo 2.1
		/// </remarks>
		[J("repository")]
		[JI(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public Uri? Repository { get; set; }
	}

	public class NodeInfoUsage
	{
		[J("users")]         public NodeInfoUsers? Users         { get; set; }
		[J("localPosts")]    public long?          LocalPosts    { get; set; }
		[J("localComments")] public long?          LocalComments { get; set; }
	}

	public class NodeInfoUsers
	{
		[J("total")]          public long? Total          { get; set; }
		[J("activeHalfyear")] public long? ActiveHalfYear { get; set; }
		[J("activeMonth")]    public long? ActiveMonth    { get; set; }
	}
}