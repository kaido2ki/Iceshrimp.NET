using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;
using JR = System.Text.Json.Serialization.JsonRequiredAttribute;
using JI = System.Text.Json.Serialization.JsonIgnoreAttribute;

namespace Iceshrimp.Backend.Core.Federation.WebFinger;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class WebFingerLink
{
	[J("rel")] [JR] public string Rel { get; set; } = null!;

	[J("type")]
	[JI(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Type { get; set; }

	[J("href")]
	[JI(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Href { get; set; }

	[J("template")]
	[JI(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Template { get; set; }
}

[SuppressMessage("ReSharper", "CollectionNeverUpdated.Global")]
[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class WebFingerResponse
{
	[J("links")] [JR]   public List<WebFingerLink> Links   { get; set; } = null!;
	[J("subject")] [JR] public string              Subject { get; set; } = null!;

	[J("aliases")]
	[JI(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public List<string>? Aliases { get; set; }
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