﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EntityFrameworkCore.Projectables;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Helpers;
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
public class User : IEntity {
	/// <summary>
	///     The created date of the User.
	/// </summary>
	[Column("createdAt")]
	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

	/// <summary>
	///     The updated date of the User.
	/// </summary>
	[Column("updatedAt")]
	public DateTime? UpdatedAt { get; set; }

	[Column("lastFetchedAt")] public DateTime? LastFetchedAt { get; set; }

	[NotMapped]
	[Projectable]
	public bool NeedsUpdate =>
		Host != null && (LastFetchedAt == null || LastFetchedAt < DateTime.Now - TimeSpan.FromHours(24));

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
	public string? DisplayName { get; set; }

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

	[NotMapped] [Projectable] public string Acct           => Username + (Host != null ? "@" + Host : "");
	[NotMapped] [Projectable] public string AcctWithPrefix => "acct:" + Acct;

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
	public List<string>? AlsoKnownAs { get; set; }

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

	[InverseProperty(nameof(AbuseUserReport.Assignee))]
	public virtual ICollection<AbuseUserReport> AbuseUserReportAssignees { get; set; } = new List<AbuseUserReport>();

	[InverseProperty(nameof(AbuseUserReport.Reporter))]
	public virtual ICollection<AbuseUserReport> AbuseUserReportReporters { get; set; } = new List<AbuseUserReport>();

	[InverseProperty(nameof(AbuseUserReport.TargetUser))]
	public virtual ICollection<AbuseUserReport> AbuseUserReportTargetUsers { get; set; } = new List<AbuseUserReport>();

	[InverseProperty(nameof(AccessToken.User))]
	public virtual ICollection<AccessToken> AccessTokens { get; set; } = new List<AccessToken>();

	[InverseProperty(nameof(AnnouncementRead.User))]
	public virtual ICollection<AnnouncementRead> AnnouncementReads { get; set; } = new List<AnnouncementRead>();

	[InverseProperty(nameof(Antenna.User))]
	public virtual ICollection<Antenna> Antennas { get; set; } = new List<Antenna>();

	[InverseProperty(nameof(App.User))] public virtual ICollection<App> Apps { get; set; } = new List<App>();

	[InverseProperty(nameof(AttestationChallenge.User))]
	public virtual ICollection<AttestationChallenge> AttestationChallenges { get; set; } =
		new List<AttestationChallenge>();

	[InverseProperty(nameof(AuthSession.User))]
	public virtual ICollection<AuthSession> AuthSessions { get; set; } = new List<AuthSession>();

	[ForeignKey("AvatarId")]
	[InverseProperty(nameof(DriveFile.UserAvatar))]
	public virtual DriveFile? Avatar { get; set; }

	[ForeignKey("BannerId")]
	[InverseProperty(nameof(DriveFile.UserBanner))]
	public virtual DriveFile? Banner { get; set; }

	[InverseProperty(nameof(Tables.Blocking.Blockee))]
	public virtual ICollection<Blocking> IncomingBlocks { get; set; } = new List<Blocking>();

	[InverseProperty(nameof(Tables.Blocking.Blocker))]
	public virtual ICollection<Blocking> OutgoingBlocks { get; set; } = new List<Blocking>();

	[NotMapped] [Projectable] public virtual IEnumerable<User> BlockedBy => IncomingBlocks.Select(p => p.Blocker);

	[NotMapped] [Projectable] public virtual IEnumerable<User> Blocking => OutgoingBlocks.Select(p => p.Blockee);

	[InverseProperty(nameof(ChannelFollowing.Follower))]
	public virtual ICollection<ChannelFollowing> ChannelFollowings { get; set; } = new List<ChannelFollowing>();

	[InverseProperty(nameof(Channel.User))]
	public virtual ICollection<Channel> Channels { get; set; } = new List<Channel>();

	[InverseProperty(nameof(Clip.User))] public virtual ICollection<Clip> Clips { get; set; } = new List<Clip>();

	[InverseProperty(nameof(DriveFile.User))]
	public virtual ICollection<DriveFile> DriveFiles { get; set; } = new List<DriveFile>();

	[InverseProperty(nameof(DriveFolder.User))]
	public virtual ICollection<DriveFolder> DriveFolders { get; set; } = new List<DriveFolder>();

	[InverseProperty(nameof(FollowRequest.Followee))]
	public virtual ICollection<FollowRequest> IncomingFollowRequests { get; set; } = new List<FollowRequest>();

	[InverseProperty(nameof(FollowRequest.Follower))]
	public virtual ICollection<FollowRequest> OutgoingFollowRequests { get; set; } = new List<FollowRequest>();

	[NotMapped]
	[Projectable]
	public virtual IEnumerable<User> ReceivedFollowRequests => IncomingFollowRequests.Select(p => p.Follower);

	[NotMapped]
	[Projectable]
	public virtual IEnumerable<User> SentFollowRequests => OutgoingFollowRequests.Select(p => p.Followee);

	[InverseProperty(nameof(Tables.Following.Followee))]
	public virtual ICollection<Following> IncomingFollowRelationships { get; set; } = new List<Following>();

	[InverseProperty(nameof(Tables.Following.Follower))]
	public virtual ICollection<Following> OutgoingFollowRelationships { get; set; } = new List<Following>();

	[NotMapped]
	[Projectable]
	public virtual IEnumerable<User> Followers => IncomingFollowRelationships.Select(p => p.Follower);

	[NotMapped]
	[Projectable]
	public virtual IEnumerable<User> Following => OutgoingFollowRelationships.Select(p => p.Followee);

	[InverseProperty(nameof(GalleryLike.User))]
	public virtual ICollection<GalleryLike> GalleryLikes { get; set; } = new List<GalleryLike>();

	[InverseProperty(nameof(GalleryPost.User))]
	public virtual ICollection<GalleryPost> GalleryPosts { get; set; } = new List<GalleryPost>();

	[InverseProperty(nameof(Tables.HtmlUserCacheEntry.User))]
	public virtual HtmlUserCacheEntry? HtmlUserCacheEntry { get; set; }

	[InverseProperty(nameof(MessagingMessage.Recipient))]
	public virtual ICollection<MessagingMessage> MessagingMessageRecipients { get; set; } =
		new List<MessagingMessage>();

	[InverseProperty(nameof(MessagingMessage.User))]
	public virtual ICollection<MessagingMessage> MessagingMessageUsers { get; set; } = new List<MessagingMessage>();

	[InverseProperty(nameof(ModerationLog.User))]
	public virtual ICollection<ModerationLog> ModerationLogs { get; set; } = new List<ModerationLog>();

	[InverseProperty(nameof(Tables.Muting.Mutee))]
	public virtual ICollection<Muting> IncomingMutes { get; set; } = new List<Muting>();

	[InverseProperty(nameof(Tables.Muting.Muter))]
	public virtual ICollection<Muting> OutgoingMutes { get; set; } = new List<Muting>();

	[NotMapped] [Projectable] public virtual IEnumerable<User> MutedBy => IncomingMutes.Select(p => p.Muter);

	[NotMapped] [Projectable] public virtual IEnumerable<User> Muting => OutgoingMutes.Select(p => p.Mutee);

	[InverseProperty(nameof(NoteBookmark.User))]
	public virtual ICollection<NoteBookmark> NoteBookmarks { get; set; } = new List<NoteBookmark>();

	[InverseProperty(nameof(NoteReaction.User))]
	public virtual ICollection<NoteLike> NoteLikes { get; set; } = new List<NoteLike>();

	[InverseProperty(nameof(NoteReaction.User))]
	public virtual ICollection<NoteReaction> NoteReactions { get; set; } = new List<NoteReaction>();

	[InverseProperty(nameof(NoteThreadMuting.User))]
	public virtual ICollection<NoteThreadMuting> NoteThreadMutings { get; set; } = new List<NoteThreadMuting>();

	[InverseProperty(nameof(NoteUnread.User))]
	public virtual ICollection<NoteUnread> NoteUnreads { get; set; } = new List<NoteUnread>();

	[InverseProperty(nameof(NoteWatching.User))]
	public virtual ICollection<NoteWatching> NoteWatchings { get; set; } = new List<NoteWatching>();

	[InverseProperty(nameof(Note.User))] public virtual ICollection<Note> Notes { get; set; } = new List<Note>();

	[InverseProperty(nameof(Notification.Notifiee))]
	public virtual ICollection<Notification> NotificationNotifiees { get; set; } = new List<Notification>();

	[InverseProperty(nameof(Notification.Notifier))]
	public virtual ICollection<Notification> NotificationNotifiers { get; set; } = new List<Notification>();

	[InverseProperty(nameof(OauthToken.User))]
	public virtual ICollection<OauthToken> OauthTokens { get; set; } = new List<OauthToken>();

	[InverseProperty(nameof(PageLike.User))]
	public virtual ICollection<PageLike> PageLikes { get; set; } = new List<PageLike>();

	[InverseProperty(nameof(Page.User))] public virtual ICollection<Page> Pages { get; set; } = new List<Page>();

	[InverseProperty(nameof(PasswordResetRequest.User))]
	public virtual ICollection<PasswordResetRequest> PasswordResetRequests { get; set; } =
		new List<PasswordResetRequest>();

	[InverseProperty(nameof(PollVote.User))]
	public virtual ICollection<PollVote> PollVotes { get; set; } = new List<PollVote>();

	[InverseProperty(nameof(PromoRead.User))]
	public virtual ICollection<PromoRead> PromoReads { get; set; } = new List<PromoRead>();

	[InverseProperty(nameof(RegistryItem.User))]
	public virtual ICollection<RegistryItem> RegistryItems { get; set; } = new List<RegistryItem>();

	[InverseProperty(nameof(RenoteMuting.Mutee))]
	public virtual ICollection<RenoteMuting> RenoteMutingMutees { get; set; } = new List<RenoteMuting>();

	[InverseProperty(nameof(RenoteMuting.Muter))]
	public virtual ICollection<RenoteMuting> RenoteMutingMuters { get; set; } = new List<RenoteMuting>();

	[InverseProperty(nameof(Session.User))]
	public virtual ICollection<Session> Sessions { get; set; } = new List<Session>();

	[InverseProperty(nameof(Signin.User))]
	public virtual ICollection<Signin> Signins { get; set; } = new List<Signin>();

	[InverseProperty(nameof(SwSubscription.User))]
	public virtual ICollection<SwSubscription> SwSubscriptions { get; set; } = new List<SwSubscription>();

	[InverseProperty(nameof(UserGroupInvitation.User))]
	public virtual ICollection<UserGroupInvitation> UserGroupInvitations { get; set; } =
		new List<UserGroupInvitation>();

	[InverseProperty(nameof(UserGroupMember.User))]
	public virtual ICollection<UserGroupMember> UserGroupMemberships { get; set; } = new List<UserGroupMember>();

	[InverseProperty(nameof(UserGroup.User))]
	public virtual ICollection<UserGroup> UserGroups { get; set; } = new List<UserGroup>();

	[InverseProperty(nameof(Tables.UserKeypair.User))]
	public virtual UserKeypair? UserKeypair { get; set; }

	[InverseProperty(nameof(UserListMember.User))]
	public virtual ICollection<UserListMember> UserListMembers { get; set; } = new List<UserListMember>();

	[InverseProperty(nameof(UserList.User))]
	public virtual ICollection<UserList> UserLists { get; set; } = new List<UserList>();

	[InverseProperty(nameof(UserNotePin.User))]
	public virtual ICollection<UserNotePin> UserNotePins { get; set; } = new List<UserNotePin>();

	[NotMapped] [Projectable] public virtual IEnumerable<Note> PinnedNotes => UserNotePins.Select(p => p.Note);

	[InverseProperty(nameof(Tables.UserProfile.User))]
	public virtual UserProfile? UserProfile { get; set; }

	[InverseProperty(nameof(Tables.UserPublickey.User))]
	public virtual UserPublickey? UserPublickey { get; set; }

	[InverseProperty(nameof(UserSecurityKey.User))]
	public virtual ICollection<UserSecurityKey> UserSecurityKeys { get; set; } = new List<UserSecurityKey>();

	[InverseProperty(nameof(Webhook.User))]
	public virtual ICollection<Webhook> Webhooks { get; set; } = new List<Webhook>();

	[NotMapped] public bool? PrecomputedIsBlocking  { get; set; }
	[NotMapped] public bool? PrecomputedIsBlockedBy { get; set; }

	[NotMapped] public bool? PrecomputedIsMuting  { get; set; }
	[NotMapped] public bool? PrecomputedIsMutedBy { get; set; }

	[NotMapped] public bool? PrecomputedIsFollowing  { get; set; }
	[NotMapped] public bool? PrecomputedIsFollowedBy { get; set; }

	[NotMapped] public bool? PrecomputedIsRequested   { get; set; }
	[NotMapped] public bool? PrecomputedIsRequestedBy { get; set; }

	[Key]
	[Column("id")]
	[StringLength(32)]
	public string Id { get; set; } = IdHelpers.GenerateSlowflakeId();

	[Projectable]
	public bool DisplayNameContainsCaseInsensitive(string str) =>
		DisplayName != null && EF.Functions.ILike(DisplayName, "%" + EfHelpers.EscapeLikeQuery(str) + "%", @"\");

	[Projectable]
	public bool UsernameContainsCaseInsensitive(string str) => UsernameLower.Contains(str);

	[Projectable]
	public bool IsBlockedBy(User user) => BlockedBy.Contains(user);

	[Projectable]
	public bool IsBlocking(User user) => Blocking.Contains(user);

	[Projectable]
	public bool IsFollowedBy(User user) => Followers.Contains(user);

	[Projectable]
	public bool IsFollowing(User user) => Following.Contains(user);

	[Projectable]
	public bool IsRequestedBy(User user) => ReceivedFollowRequests.Contains(user);

	[Projectable]
	public bool IsRequested(User user) => SentFollowRequests.Contains(user);

	[Projectable]
	public bool IsMutedBy(User user) => MutedBy.Contains(user);

	[Projectable]
	public bool IsMuting(User user) => Muting.Contains(user);

	[Projectable]
	public bool HasPinned(Note note) => PinnedNotes.Contains(note);

	public User WithPrecomputedBlockStatus(bool blocking, bool blockedBy) {
		PrecomputedIsBlocking  = blocking;
		PrecomputedIsBlockedBy = blockedBy;

		return this;
	}

	public User WithPrecomputedMuteStatus(bool muting, bool mutedBy) {
		PrecomputedIsMuting  = muting;
		PrecomputedIsMutedBy = mutedBy;

		return this;
	}

	public User WithPrecomputedFollowStatus(bool following, bool followedBy, bool requested, bool requestedBy) {
		PrecomputedIsFollowing   = following;
		PrecomputedIsFollowedBy  = followedBy;
		PrecomputedIsRequested   = requested;
		PrecomputedIsRequestedBy = requestedBy;

		return this;
	}

	public string GetPublicUrl(Config.InstanceSection config) => GetPublicUrl(config.WebDomain);
	public string GetPublicUri(Config.InstanceSection config) => GetPublicUri(config.WebDomain);
	public string GetIdenticonUrl(Config.InstanceSection config) => GetIdenticonUrl(config.WebDomain);

	public string GetPublicUri(string webDomain) => Host == null
		? $"https://{webDomain}/users/{Id}"
		: throw new Exception("Cannot access PublicUri for remote user");
	
	public string GetPublicUrl(string webDomain) => Host == null
		? $"https://{webDomain}/@{Username}"
		: throw new Exception("Cannot access PublicUrl for remote user");

	public string GetIdenticonUrl(string webDomain) => $"https://{webDomain}/identicon/{Id}";
}