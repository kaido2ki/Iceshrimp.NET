using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using NpgsqlTypes;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("user_profile")]
[Index("EnableWordMute")]
[Index("UserHost")]
[Index("PinnedPageId", IsUnique = true)]
public class UserProfile {
	[Key]
	[Column("userId")]
	[StringLength(32)]
	public string UserId { get; set; } = null!;

	/// <summary>
	///     The location of the User.
	/// </summary>
	[Column("location")]
	[StringLength(128)]
	public string? Location { get; set; }

	/// <summary>
	///     The birthday (YYYY-MM-DD) of the User.
	/// </summary>
	[Column("birthday")]
	[StringLength(10)]
	public string? Birthday { get; set; }

	/// <summary>
	///     The description (bio) of the User.
	/// </summary>
	[Column("description")]
	[StringLength(2048)]
	public string? Description { get; set; }

	[Column("fields", TypeName = "jsonb")] public string Fields { get; set; } = null!;

	/// <summary>
	///     Remote URL of the user.
	/// </summary>
	[Column("url")]
	[StringLength(512)]
	public string? Url { get; set; }
	
	[Column("ffVisibility")]
	public UserProfileFFVisibility FFVisibility { get; set; }

	[Column("mutingNotificationTypes")]
	public List<MutingNotificationType> MutingNotificationTypes { get; set; } = [];

	/// <summary>
	///     The email address of the User.
	/// </summary>
	[Column("email")]
	[StringLength(128)]
	public string? Email { get; set; }

	[Column("emailVerifyCode")]
	[StringLength(128)]
	public string? EmailVerifyCode { get; set; }

	[Column("emailVerified")] public bool EmailVerified { get; set; }

	[Column("twoFactorTempSecret")]
	[StringLength(128)]
	public string? TwoFactorTempSecret { get; set; }

	[Column("twoFactorSecret")]
	[StringLength(128)]
	public string? TwoFactorSecret { get; set; }

	[Column("twoFactorEnabled")] public bool TwoFactorEnabled { get; set; }

	/// <summary>
	///     The password hash of the User. It will be null if the origin of the user is local.
	/// </summary>
	[Column("password")]
	[StringLength(128)]
	public string? Password { get; set; }

	/// <summary>
	///     The client-specific data of the User.
	/// </summary>
	[Column("clientData", TypeName = "jsonb")]
	public string ClientData { get; set; } = null!;

	[Column("autoAcceptFollowed")] public bool AutoAcceptFollowed { get; set; }

	[Column("alwaysMarkNsfw")] public bool AlwaysMarkNsfw { get; set; }

	[Column("carefulBot")] public bool CarefulBot { get; set; }

	/// <summary>
	///     [Denormalized]
	/// </summary>
	[Column("userHost")]
	[StringLength(512)]
	public string? UserHost { get; set; }

	[Column("securityKeysAvailable")] public bool SecurityKeysAvailable { get; set; }

	[Column("usePasswordLessLogin")] public bool UsePasswordLessLogin { get; set; }

	[Column("pinnedPageId")]
	[StringLength(32)]
	public string? PinnedPageId { get; set; }

	/// <summary>
	///     The room data of the User.
	/// </summary>
	[Column("room", TypeName = "jsonb")]
	public string Room { get; set; } = null!;

	[Column("integrations", TypeName = "jsonb")]
	public string Integrations { get; set; } = null!;

	[Column("injectFeaturedNote")] public bool InjectFeaturedNote { get; set; }

	[Column("enableWordMute")] public bool EnableWordMute { get; set; }

	[Column("mutedWords", TypeName = "jsonb")]
	public string MutedWords { get; set; } = null!;

	/// <summary>
	///     Whether reject index by crawler.
	/// </summary>
	[Column("noCrawle")]
	public bool NoCrawle { get; set; }

	[Column("receiveAnnouncementEmail")] public bool ReceiveAnnouncementEmail { get; set; }

	[Column("emailNotificationTypes", TypeName = "jsonb")]
	public string EmailNotificationTypes { get; set; } = null!;

	[Column("lang")] [StringLength(32)] public string? Lang { get; set; }

	/// <summary>
	///     List of instances muted by the user.
	/// </summary>
	[Column("mutedInstances", TypeName = "jsonb")]
	public string MutedInstances { get; set; } = null!;

	[Column("publicReactions")] public bool PublicReactions { get; set; }

	[Column("moderationNote")]
	[StringLength(8192)]
	public string ModerationNote { get; set; } = null!;

	[Column("preventAiLearning")] public bool PreventAiLearning { get; set; }

	[Column("mentions", TypeName = "jsonb")]
	public string Mentions { get; set; } = null!;

	[ForeignKey("PinnedPageId")]
	[InverseProperty(nameof(Page.UserProfile))]
	public virtual Page? PinnedPage { get; set; }

	[ForeignKey("UserId")]
	[InverseProperty(nameof(Tables.User.UserProfile))]
	public virtual User User { get; set; } = null!;

	[PgName("user_profile_ffvisibility_enum")]
	public enum UserProfileFFVisibility {
		[PgName("public")]    Public,
		[PgName("followers")] Followers,
		[PgName("private")]   Private,
	}
	
	[PgName("user_profile_mutingnotificationtypes_enum")]
	public enum MutingNotificationType {
		[PgName("follow")]                Follow,
		[PgName("mention")]               Mention,
		[PgName("reply")]                 Reply,
		[PgName("renote")]                Renote,
		[PgName("quote")]                 Quote,
		[PgName("reaction")]              Reaction,
		[PgName("pollVote")]              PollVote,
		[PgName("pollEnded")]             PollEnded,
		[PgName("receiveFollowRequest")]  FollowRequestReceived,
		[PgName("followRequestAccepted")] FollowRequestAccepted,
		[PgName("groupInvited")]          GroupInvited,
		[PgName("app")]                   App,
	}
}