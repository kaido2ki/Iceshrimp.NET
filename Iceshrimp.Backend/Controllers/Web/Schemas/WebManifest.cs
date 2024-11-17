namespace Iceshrimp.Backend.Controllers.Web.Schemas;
using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;


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

	[J("icons")] public List<Icon> Icons { get; set; } =
	[
		new() { Src = "_content/Iceshrimp.Assets.Branding/512.png", Type = "image/png", Sizes = "512x512" },
		new() { Src = "_content/Iceshrimp.Assets.Branding/192.png", Type = "image/png", Sizes = "192x192" }
	];

	public class Icon
	{
		[J("src")]   public required string Src   { get; set; }
		[J("type")]  public required string Type  { get; set; }
		[J("sizes")] public required string Sizes { get; set; }
	}
}