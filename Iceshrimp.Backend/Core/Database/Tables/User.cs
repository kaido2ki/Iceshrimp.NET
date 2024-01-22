using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("user")]
[Index("Host")]
[Index("UsernameLower", "Host", IsUnique = true)]
[Index("UpdatedAt")]
[Index("UsernameLower")]
[Index("Uri")]
[Index("LastActiveDate")]
[Index("IsExplorable")]
[Index("IsAdmin")]
[Index("IsModerator")]
[Index("CreatedAt")]
[Index("Tags")]
[Index("AvatarId", IsUnique = true)]
[Index("BannerId", IsUnique = true)]
[Index("Token", IsUnique = true)]
public class User {
	[Key]
	[Column("id")]
	[StringLength(32)]
	public string Id { get; set; } = null!;

	/// <summary>
	///     The created date of the User.
	/// </summary>
	[Column("createdAt")]
	public DateTime CreatedAt { get; set; }

	/// <summary>
	///     The updated date of the User.
	/// </summary>
	[Column("updatedAt")]
	public DateTime? UpdatedAt { get; set; }

	[Column("lastFetchedAt")] public DateTime? LastFetchedAt { get; set; }

	/// <summary>
	///     The username of the User.
	/// </summary>
	[Column("username")]
	[StringLength(128)]
	public string Username { get; set; } = null!;

	/// <summary>
	///     The username (lowercased) of the User.
	/// </summary>
	[Column("usernameLower")]
	[StringLength(128)]
	public string UsernameLower { get; set; } = null!;

	/// <summary>
	///     The name of the User.
	/// </summary>
	[Column("name")]
	[StringLength(128)]
	public string? Name { get; set; }

	/// <summary>
	///     The count of followers.
	/// </summary>
	[Column("followersCount")]
	public int FollowersCount { get; set; }

	/// <summary>
	///     The count of following.
	/// </summary>
	[Column("followingCount")]
	public int FollowingCount { get; set; }

	/// <summary>
	///     The count of notes.
	/// </summary>
	[Column("notesCount")]
	public int NotesCount { get; set; }

	/// <summary>
	///     The ID of avatar DriveFile.
	/// </summary>
	[Column("avatarId")]
	[StringLength(32)]
	public string? AvatarId { get; set; }

	/// <summary>
	///     The ID of banner DriveFile.
	/// </summary>
	[Column("bannerId")]
	[StringLength(32)]
	public string? BannerId { get; set; }

	[Column("tags", TypeName = "character varying(128)[]")]
	public List<string> Tags { get; set; } = [];

	/// <summary>
	///     Whether the User is suspended.
	/// </summary>
	[Column("isSuspended")]
	public bool IsSuspended { get; set; }

	/// <summary>
	///     Whether the User is silenced.
	/// </summary>
	[Column("isSilenced")]
	public bool IsSilenced { get; set; }

	/// <summary>
	///     Whether the User is locked.
	/// </summary>
	[Column("isLocked")]
	public bool IsLocked { get; set; }

	/// <summary>
	///     Whether the User is a bot.
	/// </summary>
	[Column("isBot")]
	public bool IsBot { get; set; }

	/// <summary>
	///     Whether the User is a cat.
	/// </summary>
	[Column("isCat")]
	public bool IsCat { get; set; }

	/// <summary>
	///     Whether the User is the admin.
	/// </summary>
	[Column("isAdmin")]
	public bool IsAdmin { get; set; }

	/// <summary>
	///     Whether the User is a moderator.
	/// </summary>
	[Column("isModerator")]
	public bool IsModerator { get; set; }

	[Column("emojis", TypeName = "character varying(128)[]")]
	public List<string> Emojis { get; set; } = [];

	/// <summary>
	///     The host of the User. It will be null if the origin of the user is local.
	/// </summary>
	[Column("host")]
	[StringLength(512)]
	public string? Host { get; set; }

	/// <summary>
	///     The inbox URL of the User. It will be null if the origin of the user is local.
	/// </summary>
	[Column("inbox")]
	[StringLength(512)]
	public string? Inbox { get; set; }

	/// <summary>
	///     The sharedInbox URL of the User. It will be null if the origin of the user is local.
	/// </summary>
	[Column("sharedInbox")]
	[StringLength(512)]
	public string? SharedInbox { get; set; }

	/// <summary>
	///     The featured URL of the User. It will be null if the origin of the user is local.
	/// </summary>
	[Column("featured")]
	[StringLength(512)]
	public string? Featured { get; set; }

	/// <summary>
	///     The URI of the User. It will be null if the origin of the user is local.
	/// </summary>
	[Column("uri")]
	[StringLength(512)]
	public string? Uri { get; set; }

	/// <summary>
	///     The native access token of the User. It will be null if the origin of the user is local.
	/// </summary>
	[Column("token")]
	[StringLength(16)]
	public string? Token { get; set; }

	/// <summary>
	///     Whether the User is explorable.
	/// </summary>
	[Column("isExplorable")]
	public bool IsExplorable { get; set; }

	/// <summary>
	///     The URI of the user Follower Collection. It will be null if the origin of the user is local.
	/// </summary>
	[Column("followersUri")]
	[StringLength(512)]
	public string? FollowersUri { get; set; }

	[Column("lastActiveDate")] public DateTime? LastActiveDate { get; set; }

	[Column("hideOnlineStatus")] public bool HideOnlineStatus { get; set; }

	/// <summary>
	///     Whether the User is deleted.
	/// </summary>
	[Column("isDeleted")]
	public bool IsDeleted { get; set; }

	/// <summary>
	///     Overrides user drive capacity limit
	/// </summary>
	[Column("driveCapacityOverrideMb")]
	public int? DriveCapacityOverrideMb { get; set; }

	/// <summary>
	///     The URI of the new account of the User
	/// </summary>
	[Column("movedToUri")]
	[StringLength(512)]
	public string? MovedToUri { get; set; }

	/// <summary>
	///     URIs the user is known as too
	/// </summary>
	[Column("alsoKnownAs")]
	public string? AlsoKnownAs { get; set; }

	/// <summary>
	///     Whether to speak as a cat if isCat.
	/// </summary>
	[Column("speakAsCat")]
	public bool SpeakAsCat { get; set; }

	/// <summary>
	///     The URL of the avatar DriveFile
	/// </summary>
	[Column("avatarUrl")]
	[StringLength(512)]
	public string? AvatarUrl { get; set; }

	/// <summary>
	///     The blurhash of the avatar DriveFile
	/// </summary>
	[Column("avatarBlurhash")]
	[StringLength(128)]
	public string? AvatarBlurhash { get; set; }

	/// <summary>
	///     The URL of the banner DriveFile
	/// </summary>
	[Column("bannerUrl")]
	[StringLength(512)]
	public string? BannerUrl { get; set; }

	/// <summary>
	///     The blurhash of the banner DriveFile
	/// </summary>
	[Column("bannerBlurhash")]
	[StringLength(128)]
	public string? BannerBlurhash { get; set; }

	[InverseProperty("Assignee")]
	public virtual ICollection<AbuseUserReport> AbuseUserReportAssignees { get; set; } = new List<AbuseUserReport>();

	[InverseProperty("Reporter")]
	public virtual ICollection<AbuseUserReport> AbuseUserReportReporters { get; set; } = new List<AbuseUserReport>();

	[InverseProperty("TargetUser")]
	public virtual ICollection<AbuseUserReport> AbuseUserReportTargetUsers { get; set; } = new List<AbuseUserReport>();

	[InverseProperty("User")]
	public virtual ICollection<AccessToken> AccessTokens { get; set; } = new List<AccessToken>();

	[InverseProperty("User")]
	public virtual ICollection<AnnouncementRead> AnnouncementReads { get; set; } = new List<AnnouncementRead>();

	[InverseProperty("User")] public virtual ICollection<Antenna> Antennas { get; set; } = new List<Antenna>();

	[InverseProperty("User")] public virtual ICollection<App> Apps { get; set; } = new List<App>();

	[InverseProperty("User")]
	public virtual ICollection<AttestationChallenge> AttestationChallenges { get; set; } =
		new List<AttestationChallenge>();

	[InverseProperty("User")]
	public virtual ICollection<AuthSession> AuthSessions { get; set; } = new List<AuthSession>();

	[ForeignKey("AvatarId")]
	[InverseProperty("UserAvatar")]
	public virtual DriveFile? Avatar { get; set; }

	[ForeignKey("BannerId")]
	[InverseProperty("UserBanner")]
	public virtual DriveFile? Banner { get; set; }

	[InverseProperty("Blockee")]
	public virtual ICollection<Blocking> BlockingBlockees { get; set; } = new List<Blocking>();

	[InverseProperty("Blocker")]
	public virtual ICollection<Blocking> BlockingBlockers { get; set; } = new List<Blocking>();

	[InverseProperty("Follower")]
	public virtual ICollection<ChannelFollowing> ChannelFollowings { get; set; } = new List<ChannelFollowing>();

	[InverseProperty("User")] public virtual ICollection<Channel> Channels { get; set; } = new List<Channel>();

	[InverseProperty("User")] public virtual ICollection<Clip> Clips { get; set; } = new List<Clip>();

	[InverseProperty("User")] public virtual ICollection<DriveFile> DriveFiles { get; set; } = new List<DriveFile>();

	[InverseProperty("User")]
	public virtual ICollection<DriveFolder> DriveFolders { get; set; } = new List<DriveFolder>();

	[InverseProperty("Followee")]
	public virtual ICollection<FollowRequest> FollowRequestFollowees { get; set; } = new List<FollowRequest>();

	[InverseProperty("Follower")]
	public virtual ICollection<FollowRequest> FollowRequestFollowers { get; set; } = new List<FollowRequest>();

	[InverseProperty("Followee")]
	public virtual ICollection<Following> FollowingFollowees { get; set; } = new List<Following>();

	[InverseProperty("Follower")]
	public virtual ICollection<Following> FollowingFollowers { get; set; } = new List<Following>();

	[InverseProperty("User")]
	public virtual ICollection<GalleryLike> GalleryLikes { get; set; } = new List<GalleryLike>();

	[InverseProperty("User")]
	public virtual ICollection<GalleryPost> GalleryPosts { get; set; } = new List<GalleryPost>();

	[InverseProperty("User")] public virtual HtmlUserCacheEntry? HtmlUserCacheEntry { get; set; }

	[InverseProperty("Recipient")]
	public virtual ICollection<MessagingMessage> MessagingMessageRecipients { get; set; } =
		new List<MessagingMessage>();

	[InverseProperty("User")]
	public virtual ICollection<MessagingMessage> MessagingMessageUsers { get; set; } = new List<MessagingMessage>();

	[InverseProperty("User")]
	public virtual ICollection<ModerationLog> ModerationLogs { get; set; } = new List<ModerationLog>();

	[InverseProperty("Mutee")] public virtual ICollection<Muting> MutingMutees { get; set; } = new List<Muting>();

	[InverseProperty("Muter")] public virtual ICollection<Muting> MutingMuters { get; set; } = new List<Muting>();

	[InverseProperty("User")]
	public virtual ICollection<NoteFavorite> NoteFavorites { get; set; } = new List<NoteFavorite>();

	[InverseProperty("User")]
	public virtual ICollection<NoteReaction> NoteReactions { get; set; } = new List<NoteReaction>();

	[InverseProperty("User")]
	public virtual ICollection<NoteThreadMuting> NoteThreadMutings { get; set; } = new List<NoteThreadMuting>();

	[InverseProperty("User")] public virtual ICollection<NoteUnread> NoteUnreads { get; set; } = new List<NoteUnread>();

	[InverseProperty("User")]
	public virtual ICollection<NoteWatching> NoteWatchings { get; set; } = new List<NoteWatching>();

	[InverseProperty("User")] public virtual ICollection<Note> Notes { get; set; } = new List<Note>();

	[InverseProperty("Notifiee")]
	public virtual ICollection<Notification> NotificationNotifiees { get; set; } = new List<Notification>();

	[InverseProperty("Notifier")]
	public virtual ICollection<Notification> NotificationNotifiers { get; set; } = new List<Notification>();

	[InverseProperty("User")] public virtual ICollection<OauthToken> OauthTokens { get; set; } = new List<OauthToken>();

	[InverseProperty("User")] public virtual ICollection<PageLike> PageLikes { get; set; } = new List<PageLike>();

	[InverseProperty("User")] public virtual ICollection<Page> Pages { get; set; } = new List<Page>();

	[InverseProperty("User")]
	public virtual ICollection<PasswordResetRequest> PasswordResetRequests { get; set; } =
		new List<PasswordResetRequest>();

	[InverseProperty("User")] public virtual ICollection<PollVote> PollVotes { get; set; } = new List<PollVote>();

	[InverseProperty("User")] public virtual ICollection<PromoRead> PromoReads { get; set; } = new List<PromoRead>();

	[InverseProperty("User")]
	public virtual ICollection<RegistryItem> RegistryItems { get; set; } = new List<RegistryItem>();

	[InverseProperty("Mutee")]
	public virtual ICollection<RenoteMuting> RenoteMutingMutees { get; set; } = new List<RenoteMuting>();

	[InverseProperty("Muter")]
	public virtual ICollection<RenoteMuting> RenoteMutingMuters { get; set; } = new List<RenoteMuting>();

	[InverseProperty("User")] public virtual ICollection<Session> Sessions { get; set; } = new List<Session>();

	[InverseProperty("User")] public virtual ICollection<Signin> Signins { get; set; } = new List<Signin>();

	[InverseProperty("User")]
	public virtual ICollection<SwSubscription> SwSubscriptions { get; set; } = new List<SwSubscription>();

	[InverseProperty("User")]
	public virtual ICollection<UserGroupInvitation> UserGroupInvitations { get; set; } =
		new List<UserGroupInvitation>();

	[InverseProperty("User")]
	public virtual ICollection<UserGroupMember> UserGroupMemberships { get; set; } = new List<UserGroupMember>();

	[InverseProperty("User")] public virtual ICollection<UserGroup> UserGroups { get; set; } = new List<UserGroup>();

	[InverseProperty("User")] public virtual UserKeypair? UserKeypair { get; set; }

	[InverseProperty("User")]
	public virtual ICollection<UserListMember> UserListMembers { get; set; } = new List<UserListMember>();

	[InverseProperty("User")] public virtual ICollection<UserList> UserLists { get; set; } = new List<UserList>();

	[InverseProperty("User")]
	public virtual ICollection<UserNotePin> UserNotePins { get; set; } = new List<UserNotePin>();

	[InverseProperty("User")] public virtual UserProfile? UserProfile { get; set; }

	[InverseProperty("User")] public virtual UserPublickey? UserPublickey { get; set; }

	[InverseProperty("User")]
	public virtual ICollection<UserSecurityKey> UserSecurityKeys { get; set; } = new List<UserSecurityKey>();

	[InverseProperty("User")] public virtual ICollection<Webhook> Webhooks { get; set; } = new List<Webhook>();
}