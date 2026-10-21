namespace Iceshrimp.Backend.Core.Configuration;

public static class Constants
{
	// To ensure compliance with the license of this project, adjust the link below if you are distributing a modified copy
	public const string RepositoryUrl = "https://iceshrimp.dev/iceshrimp/Iceshrimp.NET";

	// When a git commit ID is appended, this URL should point to the source tree as that commit. Used by Akkoma-FE
	public const string CommitUrl = $"{RepositoryUrl}/src/commit/";

	// Leave this as-is if you're running upstream code, or your fork does not have any meaningful behavioral changes, otherwise please link to your own issue tracker here
	public const string IssueTrackerUrl = "https://issues.iceshrimp.dev";

	// Leave this as-is, unless you've forked the project & want to reference your own website / repository (irrelevant for license compliance)
	public const string ProjectHomepageUrl = "https://iceshrimp.dev/iceshrimp/Iceshrimp.NET";
	
	// Make sure to also update VersionHelpers.VersionIdentifier if you are building a fork

	public const           string   ActivityStreamsNs = "https://www.w3.org/ns/activitystreams";
	public const           string   W3IdSecurityNs    = "https://w3id.org/security";
	public const           string   PurlDcNs          = "http://purl.org/dc/terms";
	public const           string   XsdNs             = "http://www.w3.org/2001/XMLSchema";
	public const           string   SchemaNs          = "http://schema.org";
	public const           string   MastodonNs        = "http://joinmastodon.org/ns";
	public const           string   MisskeyNs         = "https://misskey-hub.net/ns";
	public const           string   FedibirdNs        = "http://fedibird.com/ns";
	public const           string   GoToSocialNs      = "https://gotosocial.org/ns";
	public const           string   FepNs             = "https://w3id.org/fep";
	public static readonly string[] SystemUsers       = ["instance.actor", "relay.actor"];

	public const string APMime       = "application/activity+json";
	public const string ASMime       = "application/ld+json; profile=\"https://www.w3.org/ns/activitystreams\"";

	public static readonly string[] BrowserSafeMimeTypes =
	[
		"image/png",
		"image/gif",
		"image/jpeg",
		"image/webp",
		"image/apng",
		"image/bmp",
		"image/tiff",
		"image/x-icon",
		"image/avif",
		"image/heic",
		"audio/opus",
		"video/ogg",
		"audio/ogg",
		"application/ogg",
		"video/quicktime",
		"video/mp4",
		"video/vnd.avi",
		"audio/mp4",
		"video/x-m4v",
		"audio/x-m4a",
		"video/3gpp",
		"video/3gpp2",
		"video/3gp2",
		"audio/3gpp",
		"audio/3gpp2",
		"audio/3gp2",
		"video/mpeg",
		"audio/mpeg",
		"video/webm",
		"audio/webm",
		"audio/aac",
		"audio/x-flac",
		"audio/flac",
		"audio/vnd.wave",
		"audio/wav",
		"audio/x-wav",
		"application/x-shockwave-flash",
		"application/octet-stream"
	];

	[Flags]
	public enum MastodonRolePermissions
	{
		// this list is from mastodon source code: https://github.com/mastodon/mastodon/blob/main/app/models/user_role.rb#L19
		Administrator            = 1 << 0,
		ViewDevops               = 1 << 1,
		ViewAuditLog             = 1 << 2,
		ViewDashboard            = 1 << 3,
		ManageReports            = 1 << 4,
		ManageFederation         = 1 << 5,
		ManageSettings           = 1 << 6,
		ManageBlocks             = 1 << 7,
		ManageTaxonomies         = 1 << 8,
		ManageAppeals            = 1 << 9,
		ManageUsers              = 1 << 10,
		ManageInvites            = 1 << 11,
		ManageRules              = 1 << 12,
		ManageAnnouncements      = 1 << 13,
		ManageCustomEmojis       = 1 << 14,
		ManageWebhooks           = 1 << 15,
		InviteUsers              = 1 << 16,
		ManageRoles              = 1 << 17,
		ManageUserAccess         = 1 << 18,
		DeleteUserData           = 1 << 19,
		ViewFeeds                = 1 << 20,
		InviteBypassApproval     = 1 << 21,
		ManageEmailSubscriptions = 1 << 22,
		
		// these permissions approximate mod/admin roles based off what Iceshrimp currently supports.
		// if we add more mod/admin functions it would be a good idea to check if these needs updating.
		ModeratorRole = ManageReports | ManageUsers | ManageAnnouncements | ManageCustomEmojis | DeleteUserData,
		AdminRole = ViewDashboard | ManageFederation | ManageSettings | ManageInvites | ManageRules | ModeratorRole
	}
}
