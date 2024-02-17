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
	[J("version")]           public required string           Version           { get; set; }
	[J("software")]          public required NodeInfoSoftware Software          { get; set; }
	[J("protocols")]         public required List<string>     Protocols         { get; set; }
	[J("services")]          public required NodeInfoServices Services          { get; set; }
	[J("usage")]             public required NodeInfoUsage    Usage             { get; set; }
	[J("metadata")]          public required NodeInfoMetadata Metadata          { get; set; }
	[J("openRegistrations")] public required bool             OpenRegistrations { get; set; }

	public class NodeInfoMetadata
	{
		[J("nodeName")]                   public required string       NodeName                   { get; set; }
		[J("nodeDescription")]            public required string       NodeDescription            { get; set; }
		[J("maintainer")]                 public required Maintainer   Maintainer                 { get; set; }
		[J("langs")]                      public required List<object> Languages                  { get; set; }
		[J("tosUrl")]                     public required object       TosUrl                     { get; set; }
		[J("repositoryUrl")]              public required Uri          RepositoryUrl              { get; set; }
		[J("feedbackUrl")]                public required Uri          FeedbackUrl                { get; set; }
		[J("themeColor")]                 public required string       ThemeColor                 { get; set; }
		[J("disableRegistration")]        public required bool         DisableRegistration        { get; set; }
		[J("disableLocalTimeline")]       public required bool         DisableLocalTimeline       { get; set; }
		[J("disableRecommendedTimeline")] public required bool         DisableRecommendedTimeline { get; set; }
		[J("disableGlobalTimeline")]      public required bool         DisableGlobalTimeline      { get; set; }
		[J("emailRequiredForSignup")]     public required bool         EmailRequiredForSignup     { get; set; }
		[J("postEditing")]                public required bool         PostEditing                { get; set; }
		[J("postImports")]                public required bool         PostImports                { get; set; }
		[J("enableHcaptcha")]             public required bool         EnableHCaptcha             { get; set; }
		[J("enableRecaptcha")]            public required bool         EnableRecaptcha            { get; set; }
		[J("maxNoteTextLength")]          public required long         MaxNoteTextLength          { get; set; }
		[J("maxCaptionTextLength")]       public required long         MaxCaptionTextLength       { get; set; }
		[J("enableGithubIntegration")]    public required bool         EnableGithubIntegration    { get; set; }
		[J("enableDiscordIntegration")]   public required bool         EnableDiscordIntegration   { get; set; }
		[J("enableEmail")]                public required bool         EnableEmail                { get; set; }
	}

	public class Maintainer
	{
		[J("name")]  public required string Name  { get; set; }
		[J("email")] public required string Email { get; set; }
	}

	public class NodeInfoServices
	{
		[J("inbound")]  public required List<object> Inbound  { get; set; }
		[J("outbound")] public required List<string> Outbound { get; set; }
	}

	public class NodeInfoSoftware
	{
		[J("name")]     public required string Name     { get; set; }
		[J("version")]  public required string Version  { get; set; }
		[J("homepage")] public required Uri    Homepage { get; set; }

		/// <remarks>
		///     This is only part of nodeinfo 2.1
		/// </remarks>
		[J("repository")]
		[JI(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public Uri? Repository { get; set; }
	}

	public class NodeInfoUsage
	{
		[J("users")]         public required NodeInfoUsers Users         { get; set; }
		[J("localPosts")]    public required long          LocalPosts    { get; set; }
		[J("localComments")] public required long          LocalComments { get; set; }
	}

	public class NodeInfoUsers
	{
		[J("total")]          public required long Total          { get; set; }
		[J("activeHalfyear")] public required long ActiveHalfYear { get; set; }
		[J("activeMonth")]    public required long ActiveMonth    { get; set; }
	}
}