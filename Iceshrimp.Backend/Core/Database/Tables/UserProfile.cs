using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using NpgsqlTypes;
using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("user_profile")]
[Index(nameof(EnableWordMute))]
[Index(nameof(UserHost))]
[Index("PinnedPageId", IsUnique = true)]
public class UserProfile
{
	[PgName("user_profile_ffvisibility_enum")]
	public enum UserProfileFFVisibility
	{
		[PgName("public")]    Public,
		[PgName("followers")] Followers,
		[PgName("private")]   Private
	}

	[Column("mentionsResolved")] public bool MentionsResolved;

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

	[Column("fields", TypeName = "jsonb")] public Field[] Fields { get; set; } = null!;

	/// <summary>
	///     Remote URL of the user.
	/// </summary>
	[Column("url")]
	[StringLength(512)]
	public string? Url { get; set; }

	[Column("ffVisibility")] public UserProfileFFVisibility FFVisibility { get; set; }

	[Column("mutingNotificationTypes")]
	public List<Notification.NotificationType> MutingNotificationTypes { get; set; } = [];

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
	//TODO: refactor this column (it's currently a Dictionary<string, any>, which is terrible) 
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
	//TODO: refactor this column (it's currently a Dictionary<string, any>, which is terrible) 
	[Column("room", TypeName = "jsonb")]
	public string Room { get; set; } = null!;

	//TODO: refactor this column (it's currently a Dictionary<string, any>, which is terrible) 
	[Column("integrations", TypeName = "jsonb")]
	public string Integrations { get; set; } = null!;

	[Column("injectFeaturedNote")] public bool InjectFeaturedNote { get; set; }

	[Column("enableWordMute")] public bool EnableWordMute { get; set; }

	[Column("mutedWords", TypeName = "jsonb")]
	public List<List<string>> MutedWords { get; set; } = null!;

	/// <summary>
	///     Whether reject index by crawler.
	/// </summary>
	[Column("noCrawle")]
	public bool NoCrawle { get; set; }

	[Column("receiveAnnouncementEmail")] public bool ReceiveAnnouncementEmail { get; set; }

	//TODO: refactor this column (this should have been NotificationTypeEnum[])
	[Column("emailNotificationTypes", TypeName = "jsonb")]
	public List<string> EmailNotificationTypes { get; set; } = null!;

	[Column("lang")] [StringLength(32)] public string? Lang { get; set; }

	/// <summary>
	///     List of instances muted by the user.
	/// </summary>
	//TODO: refactor this column (this should have been a varchar[]) 
	[Column("mutedInstances", TypeName = "jsonb")]
	public List<string> MutedInstances { get; set; } = null!;

	[Column("publicReactions")] public bool PublicReactions { get; set; }

	[Column("moderationNote")]
	[StringLength(8192)]
	public string ModerationNote { get; set; } = null!;

	[Column("preventAiLearning")] public bool PreventAiLearning { get; set; }

	[Column("mentions", TypeName = "jsonb")]
	public List<Note.MentionedUser> Mentions { get; set; } = null!;

	[ForeignKey(nameof(PinnedPageId))]
	[InverseProperty(nameof(Page.UserProfile))]
	public virtual Page? PinnedPage { get; set; }

	[ForeignKey(nameof(UserId))]
	[InverseProperty(nameof(Tables.User.UserProfile))]
	public virtual User User { get; set; } = null!;

	public class Field
	{
		[J("name")]     public required string Name       { get; set; }
		[J("value")]    public required string Value      { get; set; }
		[J("verified")] public          bool?  IsVerified { get; set; }
	}
}