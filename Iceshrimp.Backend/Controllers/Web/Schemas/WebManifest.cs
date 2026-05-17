using System.Text.Json.Serialization;
using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;
using JI = System.Text.Json.Serialization.JsonIgnoreAttribute;

namespace Iceshrimp.Backend.Controllers.Web.Schemas;

public class WebManifest
{
	[J("name")]                        public required string Name                      { get; set; }
	[J("short_name")]                  public required string ShortName                 { get; set; }
	[J("id")]                          public          string Id                        { get; set; } = "./";
	[J("start_url")]                   public          string StartUrl                  { get; set; } = "./";
	[J("display")]                     public          string Display                   { get; set; } = "standalone";
	[J("background_color")]            public          string BackgroundColor           { get; set; } = "#ffffff";
	[J("theme_color")]                 public          string ThemeColor                { get; set; } = "#03173d";
	[J("prefer_related_applications")] public          bool   PreferRelatedApplications { get; set; } = false;
	[J("share_target")]                public          Share  ShareTarget               { get; set; } = new();

	[J("icons")] public List<Icon> Icons { get; set; } =
	[
		new() { Src = "_content/Iceshrimp.Assets.Branding/maskable.png", Type = "image/png", Sizes = "512x512", Purpose = "maskable" },
		new() { Src = "_content/Iceshrimp.Assets.Branding/monochrome.png", Type = "image/png", Sizes = "512x512", Purpose = "monochrome" },
		new() { Src = "_content/Iceshrimp.Assets.Branding/512.png", Type = "image/png", Sizes = "512x512" },
		new() { Src = "_content/Iceshrimp.Assets.Branding/192.png", Type = "image/png", Sizes = "192x192" }
	];

	[J("shortcuts")]
	public List<Shortcut> Shortcuts { get; set; } =
	[
		new()
		{
			Name      = "New note",
			ShortName = "Note",
			Url       = "/share",
			Icons =
			[
				new()
				{
					Src     = "/assets/shortcut-note.png",
					Type    = "image/png",
					Sizes   = "96x96",
					Purpose = "monochrome"
				}
			]
		},
		new()
		{
			Name = "Notifications",
			Url  = "/notifications",
			Icons =
			[
				new()
				{
					Src     = "/assets/shortcut-notifications.png",
					Type    = "image/png",
					Sizes   = "96x96",
					Purpose = "monochrome"
				}
			]
		},
		new()
		{
			Name = "Bookmarks",
			Url  = "/bookmarks",
			Icons =
			[
				new()
				{
					Src     = "/assets/shortcut-bookmarks.png",
					Type    = "image/png",
					Sizes   = "96x96",
					Purpose = "monochrome"
				}
			]
		},
		new()
		{
			Name      = "Follow requests",
			ShortName = "Requests",
			Url       = "/follow-requests",
			Icons =
			[
				new()
				{
					Src     = "/assets/shortcut-follow-requests.png",
					Type    = "image/png",
					Sizes   = "96x96",
					Purpose = "monochrome"
				}
			]
		}
	];

	public class Icon
	{
		[J("src")]   public required string Src   { get; set; }
		[J("type")]  public required string Type  { get; set; }
		[J("sizes")] public required string Sizes { get; set; }

		[J("purpose")]
		[JI(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public string? Purpose { get; set; }
	}

	public class Share
	{
		[J("action")]  public string      Action  { get; set; } = "/share";
		[J("enctype")] public string      EncType { get; set; } = "application/x-www-form-urlencoded";
		[J("method")]  public string      Method  { get; set; } = "GET";
		[J("params")]  public ShareParams Params  { get; set; } = new();

		public class ShareParams
		{
			[J("title")] public string Title { get; set; } = "title";
			[J("text")]  public string Text  { get; set; } = "text";
			[J("url")]   public string Url   { get; set; } = "url";
		}
	}

	public class Shortcut
	{
		[J("name")] public required string Name { get; set; }

		[J("short_name")]
		[JI(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public string? ShortName { get; set; }

		[J("description")]
		[JI(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public string? Description { get; set; }

		[J("url")] public required string Url { get; set; }

		[J("icons")]
		[JI(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public List<Icon>? Icons { get; set; }
	}
}