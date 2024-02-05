using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("meta")]
public class Meta {
	[Key]
	[Column("id")]
	[StringLength(32)]
	public string Id { get; set; } = null!;

	[Column("name")] [StringLength(128)] public string? Name { get; set; }

	[Column("description")]
	[StringLength(1024)]
	public string? Description { get; set; }

	[Column("maintainerName")]
	[StringLength(128)]
	public string? MaintainerName { get; set; }

	[Column("maintainerEmail")]
	[StringLength(128)]
	public string? MaintainerEmail { get; set; }

	[Column("disableRegistration")] public bool DisableRegistration { get; set; }

	[Column("disableLocalTimeline")] public bool DisableLocalTimeline { get; set; }

	[Column("disableGlobalTimeline")] public bool DisableGlobalTimeline { get; set; }

	[Column("langs", TypeName = "character varying(64)[]")]
	public List<string> Langs { get; set; } = null!;

	[Column("hiddenTags", TypeName = "character varying(256)[]")]
	public List<string> HiddenTags { get; set; } = null!;

	[Column("blockedHosts", TypeName = "character varying(256)[]")]
	public List<string> BlockedHosts { get; set; } = null!;

	[Column("mascotImageUrl")]
	[StringLength(512)]
	public string? MascotImageUrl { get; set; }

	[Column("bannerUrl")]
	[StringLength(512)]
	public string? BannerUrl { get; set; }

	[Column("errorImageUrl")]
	[StringLength(512)]
	public string? ErrorImageUrl { get; set; }

	[Column("iconUrl")]
	[StringLength(512)]
	public string? IconUrl { get; set; }

	[Column("cacheRemoteFiles")] public bool CacheRemoteFiles { get; set; }

	[Column("enableRecaptcha")] public bool EnableRecaptcha { get; set; }

	[Column("recaptchaSiteKey")]
	[StringLength(64)]
	public string? RecaptchaSiteKey { get; set; }

	[Column("recaptchaSecretKey")]
	[StringLength(64)]
	public string? RecaptchaSecretKey { get; set; }

	/// <summary>
	///     Drive capacity of a local user (MB)
	/// </summary>
	[Column("localDriveCapacityMb")]
	public int LocalDriveCapacityMb { get; set; }

	/// <summary>
	///     Drive capacity of a remote user (MB)
	/// </summary>
	[Column("remoteDriveCapacityMb")]
	public int RemoteDriveCapacityMb { get; set; }

	[Column("summalyProxy")]
	[StringLength(128)]
	public string? SummalyProxy { get; set; }

	[Column("enableEmail")] public bool EnableEmail { get; set; }

	[Column("email")] [StringLength(128)] public string? Email { get; set; }

	[Column("smtpSecure")] public bool SmtpSecure { get; set; }

	[Column("smtpHost")]
	[StringLength(128)]
	public string? SmtpHost { get; set; }

	[Column("smtpPort")] public int? SmtpPort { get; set; }

	[Column("smtpUser")]
	[StringLength(1024)]
	public string? SmtpUser { get; set; }

	[Column("smtpPass")]
	[StringLength(1024)]
	public string? SmtpPass { get; set; }

	[Column("swPublicKey")]
	[StringLength(128)]
	public string SwPublicKey { get; set; } = null!;

	[Column("swPrivateKey")]
	[StringLength(128)]
	public string SwPrivateKey { get; set; } = null!;

	[Column("enableGithubIntegration")] public bool EnableGithubIntegration { get; set; }

	[Column("githubClientId")]
	[StringLength(128)]
	public string? GithubClientId { get; set; }

	[Column("githubClientSecret")]
	[StringLength(128)]
	public string? GithubClientSecret { get; set; }

	[Column("enableDiscordIntegration")] public bool EnableDiscordIntegration { get; set; }

	[Column("discordClientId")]
	[StringLength(128)]
	public string? DiscordClientId { get; set; }

	[Column("discordClientSecret")]
	[StringLength(128)]
	public string? DiscordClientSecret { get; set; }

	[Column("pinnedUsers", TypeName = "character varying(256)[]")]
	public List<string> PinnedUsers { get; set; } = null!;

	[Column("ToSUrl")] [StringLength(512)] public string? ToSurl { get; set; }

	[Column("repositoryUrl")]
	[StringLength(512)]
	public string RepositoryUrl { get; set; } = null!;

	[Column("feedbackUrl")]
	[StringLength(512)]
	public string? FeedbackUrl { get; set; }

	[Column("useObjectStorage")] public bool UseObjectStorage { get; set; }

	[Column("objectStorageBucket")]
	[StringLength(512)]
	public string? ObjectStorageBucket { get; set; }

	[Column("objectStoragePrefix")]
	[StringLength(512)]
	public string? ObjectStoragePrefix { get; set; }

	[Column("objectStorageBaseUrl")]
	[StringLength(512)]
	public string? ObjectStorageBaseUrl { get; set; }

	[Column("objectStorageEndpoint")]
	[StringLength(512)]
	public string? ObjectStorageEndpoint { get; set; }

	[Column("objectStorageRegion")]
	[StringLength(512)]
	public string? ObjectStorageRegion { get; set; }

	[Column("objectStorageAccessKey")]
	[StringLength(512)]
	public string? ObjectStorageAccessKey { get; set; }

	[Column("objectStorageSecretKey")]
	[StringLength(512)]
	public string? ObjectStorageSecretKey { get; set; }

	[Column("objectStoragePort")] public int? ObjectStoragePort { get; set; }

	[Column("objectStorageUseSSL")] public bool ObjectStorageUseSsl { get; set; }

	[Column("objectStorageUseProxy")] public bool ObjectStorageUseProxy { get; set; }

	[Column("enableHcaptcha")] public bool EnableHcaptcha { get; set; }

	[Column("hcaptchaSiteKey")]
	[StringLength(64)]
	public string? HcaptchaSiteKey { get; set; }

	[Column("hcaptchaSecretKey")]
	[StringLength(64)]
	public string? HcaptchaSecretKey { get; set; }

	[Column("objectStorageSetPublicRead")] public bool ObjectStorageSetPublicRead { get; set; }

	[Column("pinnedPages", TypeName = "character varying(512)[]")]
	public List<string> PinnedPages { get; set; } = null!;

	[Column("backgroundImageUrl")]
	[StringLength(512)]
	public string? BackgroundImageUrl { get; set; }

	[Column("logoImageUrl")]
	[StringLength(512)]
	public string? LogoImageUrl { get; set; }

	[Column("pinnedClipId")]
	[StringLength(32)]
	public string? PinnedClipId { get; set; }

	[Column("objectStorageS3ForcePathStyle")]
	public bool ObjectStorageS3ForcePathStyle { get; set; }

	[Column("allowedHosts", TypeName = "character varying(256)[]")]
	public List<string> AllowedHosts { get; set; } = null!;

	[Column("secureMode")] public bool SecureMode { get; set; }

	[Column("privateMode")] public bool PrivateMode { get; set; }

	[Column("deeplAuthKey")]
	[StringLength(128)]
	public string? DeeplAuthKey { get; set; }

	[Column("deeplIsPro")] public bool DeeplIsPro { get; set; }

	[Column("emailRequiredForSignup")] public bool EmailRequiredForSignup { get; set; }

	[Column("themeColor")]
	[StringLength(512)]
	public string? ThemeColor { get; set; }

	[Column("defaultLightTheme")]
	[StringLength(8192)]
	public string? DefaultLightTheme { get; set; }

	[Column("defaultDarkTheme")]
	[StringLength(8192)]
	public string? DefaultDarkTheme { get; set; }

	[Column("enableIpLogging")] public bool EnableIpLogging { get; set; }

	[Column("enableActiveEmailValidation")]
	public bool EnableActiveEmailValidation { get; set; }

	[Column("customMOTD", TypeName = "character varying(256)[]")]
	public List<string> CustomMotd { get; set; } = null!;

	[Column("customSplashIcons", TypeName = "character varying(256)[]")]
	public List<string> CustomSplashIcons { get; set; } = null!;

	[Column("disableRecommendedTimeline")] public bool DisableRecommendedTimeline { get; set; }

	[Column("recommendedInstances", TypeName = "character varying(256)[]")]
	public List<string> RecommendedInstances { get; set; } = null!;

	[Column("defaultReaction")]
	[StringLength(256)]
	public string DefaultReaction { get; set; } = null!;

	[Column("libreTranslateApiUrl")]
	[StringLength(512)]
	public string? LibreTranslateApiUrl { get; set; }

	[Column("libreTranslateApiKey")]
	[StringLength(128)]
	public string? LibreTranslateApiKey { get; set; }

	[Column("silencedHosts", TypeName = "character varying(256)[]")]
	public List<string> SilencedHosts { get; set; } = null!;

	[Column("experimentalFeatures", TypeName = "jsonb")]
	public Dictionary<string, bool> ExperimentalFeatures { get; set; } = null!;

	[Column("enableServerMachineStats")] public bool EnableServerMachineStats { get; set; }

	[Column("enableIdenticonGeneration")] public bool EnableIdenticonGeneration { get; set; }

	[Column("donationLink")]
	[StringLength(256)]
	public string? DonationLink { get; set; }

	[Column("autofollowedAccount")]
	[StringLength(128)]
	public string? AutofollowedAccount { get; set; }
}