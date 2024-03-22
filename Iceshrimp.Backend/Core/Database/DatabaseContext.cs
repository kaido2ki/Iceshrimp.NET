using System.Diagnostics.CodeAnalysis;
using EntityFramework.Exceptions.PostgreSQL;
using EntityFrameworkCore.Projectables.Infrastructure;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Npgsql;

namespace Iceshrimp.Backend.Core.Database;

[SuppressMessage("ReSharper", "StringLiteralTypo")]
[SuppressMessage("ReSharper", "IdentifierTypo")]
public class DatabaseContext(DbContextOptions<DatabaseContext> options)
	: DbContext(options), IDataProtectionKeyContext
{
	public virtual DbSet<AbuseUserReport>      AbuseUserReports      { get; init; } = null!;
	public virtual DbSet<Announcement>         Announcements         { get; init; } = null!;
	public virtual DbSet<AnnouncementRead>     AnnouncementReads     { get; init; } = null!;
	public virtual DbSet<Antenna>              Antennas              { get; init; } = null!;
	public virtual DbSet<AttestationChallenge> AttestationChallenges { get; init; } = null!;
	public virtual DbSet<Bite>                 Bites                 { get; init; } = null!;
	public virtual DbSet<Blocking>             Blockings             { get; init; } = null!;
	public virtual DbSet<Channel>              Channels              { get; init; } = null!;
	public virtual DbSet<ChannelFollowing>     ChannelFollowings     { get; init; } = null!;
	public virtual DbSet<ChannelNotePin>       ChannelNotePins       { get; init; } = null!;
	public virtual DbSet<Clip>                 Clips                 { get; init; } = null!;
	public virtual DbSet<ClipNote>             ClipNotes             { get; init; } = null!;
	public virtual DbSet<DriveFile>            DriveFiles            { get; init; } = null!;
	public virtual DbSet<DriveFolder>          DriveFolders          { get; init; } = null!;
	public virtual DbSet<Emoji>                Emojis                { get; init; } = null!;
	public virtual DbSet<FollowRequest>        FollowRequests        { get; init; } = null!;
	public virtual DbSet<Following>            Followings            { get; init; } = null!;
	public virtual DbSet<GalleryLike>          GalleryLikes          { get; init; } = null!;
	public virtual DbSet<GalleryPost>          GalleryPosts          { get; init; } = null!;
	public virtual DbSet<Hashtag>              Hashtags              { get; init; } = null!;
	public virtual DbSet<Instance>             Instances             { get; init; } = null!;
	public virtual DbSet<Marker>               Markers               { get; init; } = null!;
	public virtual DbSet<MessagingMessage>     MessagingMessages     { get; init; } = null!;
	public virtual DbSet<Meta>                 Meta                  { get; init; } = null!;
	public virtual DbSet<ModerationLog>        ModerationLogs        { get; init; } = null!;
	public virtual DbSet<Muting>               Mutings               { get; init; } = null!;
	public virtual DbSet<Note>                 Notes                 { get; init; } = null!;
	public virtual DbSet<NoteBookmark>         NoteBookmarks         { get; init; } = null!;
	public virtual DbSet<NoteEdit>             NoteEdits             { get; init; } = null!;
	public virtual DbSet<NoteLike>             NoteLikes             { get; init; } = null!;
	public virtual DbSet<NoteReaction>         NoteReactions         { get; init; } = null!;
	public virtual DbSet<NoteThreadMuting>     NoteThreadMutings     { get; init; } = null!;
	public virtual DbSet<NoteUnread>           NoteUnreads           { get; init; } = null!;
	public virtual DbSet<NoteWatching>         NoteWatchings         { get; init; } = null!;
	public virtual DbSet<Notification>         Notifications         { get; init; } = null!;
	public virtual DbSet<OauthApp>             OauthApps             { get; init; } = null!;
	public virtual DbSet<OauthToken>           OauthTokens           { get; init; } = null!;
	public virtual DbSet<Page>                 Pages                 { get; init; } = null!;
	public virtual DbSet<PageLike>             PageLikes             { get; init; } = null!;
	public virtual DbSet<PasswordResetRequest> PasswordResetRequests { get; init; } = null!;
	public virtual DbSet<Poll>                 Polls                 { get; init; } = null!;
	public virtual DbSet<PollVote>             PollVotes             { get; init; } = null!;
	public virtual DbSet<PromoNote>            PromoNotes            { get; init; } = null!;
	public virtual DbSet<PromoRead>            PromoReads            { get; init; } = null!;
	public virtual DbSet<RegistrationInvite>   RegistrationInvites   { get; init; } = null!;
	public virtual DbSet<RegistryItem>         RegistryItems         { get; init; } = null!;
	public virtual DbSet<Relay>                Relays                { get; init; } = null!;
	public virtual DbSet<RenoteMuting>         RenoteMutings         { get; init; } = null!;
	public virtual DbSet<Session>              Sessions              { get; init; } = null!;
	public virtual DbSet<SwSubscription>       SwSubscriptions       { get; init; } = null!;
	public virtual DbSet<PushSubscription>     PushSubscriptions     { get; init; } = null!;
	public virtual DbSet<UsedUsername>         UsedUsernames         { get; init; } = null!;
	public virtual DbSet<User>                 Users                 { get; init; } = null!;
	public virtual DbSet<UserGroup>            UserGroups            { get; init; } = null!;
	public virtual DbSet<UserGroupInvitation>  UserGroupInvitations  { get; init; } = null!;
	public virtual DbSet<UserGroupMember>      UserGroupMembers      { get; init; } = null!;
	public virtual DbSet<UserKeypair>          UserKeypairs          { get; init; } = null!;
	public virtual DbSet<UserList>             UserLists             { get; init; } = null!;
	public virtual DbSet<UserListMember>       UserListMembers       { get; init; } = null!;
	public virtual DbSet<UserNotePin>          UserNotePins          { get; init; } = null!;
	public virtual DbSet<UserPending>          UserPendings          { get; init; } = null!;
	public virtual DbSet<UserProfile>          UserProfiles          { get; init; } = null!;
	public virtual DbSet<UserPublickey>        UserPublickeys        { get; init; } = null!;
	public virtual DbSet<UserSecurityKey>      UserSecurityKeys      { get; init; } = null!;
	public virtual DbSet<UserSettings>         UserSettings          { get; init; } = null!;
	public virtual DbSet<Webhook>              Webhooks              { get; init; } = null!;
	public virtual DbSet<AllowedInstance>      AllowedInstances      { get; init; } = null!;
	public virtual DbSet<BlockedInstance>      BlockedInstances      { get; init; } = null!;
	public virtual DbSet<MetaStoreEntry>       MetaStore             { get; init; } = null!;
	public virtual DbSet<DataProtectionKey>    DataProtectionKeys    { get; init; } = null!;

	public static NpgsqlDataSource GetDataSource(Config.DatabaseSection? config)
	{
		var dataSourceBuilder = new NpgsqlDataSourceBuilder();

		if (config == null)
			throw new Exception("Failed to initialize database: Failed to load configuration");

		dataSourceBuilder.ConnectionStringBuilder.Host     = config.Host;
		dataSourceBuilder.ConnectionStringBuilder.Port     = config.Port;
		dataSourceBuilder.ConnectionStringBuilder.Username = config.Username;
		dataSourceBuilder.ConnectionStringBuilder.Password = config.Password;
		dataSourceBuilder.ConnectionStringBuilder.Database = config.Database;

		return ConfigureDataSource(dataSourceBuilder);
	}

	public static NpgsqlDataSource ConfigureDataSource(NpgsqlDataSourceBuilder dataSourceBuilder)
	{
		dataSourceBuilder.MapEnum<Antenna.AntennaSource>();
		dataSourceBuilder.MapEnum<Note.NoteVisibility>();
		dataSourceBuilder.MapEnum<Notification.NotificationType>();
		dataSourceBuilder.MapEnum<Page.PageVisibility>();
		dataSourceBuilder.MapEnum<Relay.RelayStatus>();
		dataSourceBuilder.MapEnum<UserProfile.UserProfileFFVisibility>();
		dataSourceBuilder.MapEnum<Marker.MarkerType>();
		dataSourceBuilder.MapEnum<PushSubscription.PushPolicy>();

		dataSourceBuilder.EnableDynamicJson();

		return dataSourceBuilder.Build();
	}

	public static void Configure(DbContextOptionsBuilder optionsBuilder, NpgsqlDataSource dataSource)
	{
		optionsBuilder.UseNpgsql(dataSource);
		optionsBuilder.UseProjectables(options => { options.CompatibilityMode(CompatibilityMode.Full); });
		optionsBuilder.UseExceptionProcessor();
	}

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder
			.HasPostgresEnum<Antenna.AntennaSource>()
			.HasPostgresEnum<Note.NoteVisibility>()
			.HasPostgresEnum<Notification.NotificationType>()
			.HasPostgresEnum<Page.PageVisibility>()
			.HasPostgresEnum<Relay.RelayStatus>()
			.HasPostgresEnum<UserProfile.UserProfileFFVisibility>()
			.HasPostgresEnum<Marker.MarkerType>()
			.HasPostgresEnum<PushSubscription.PushPolicy>()
			.HasPostgresExtension("pg_trgm");

		modelBuilder
			.HasDbFunction(typeof(DatabaseContext).GetMethod(nameof(NoteAncestors),
			                                                 [typeof(string), typeof(int)])!)
			.HasName("note_ancestors");
		modelBuilder
			.HasDbFunction(typeof(DatabaseContext).GetMethod(nameof(NoteDescendants),
			                                                 [typeof(string), typeof(int), typeof(int)])!)
			.HasName("note_descendants");
		modelBuilder
			.HasDbFunction(typeof(DatabaseContext).GetMethod(nameof(Conversations),
			                                                 [typeof(string)])!)
			.HasName("conversations");
		modelBuilder
			.HasDbFunction(typeof(Note).GetMethod(nameof(Note.InternalRawAttachments),
			                                      [typeof(string)])!)
			.HasName("note_attachments_raw");

		modelBuilder.Entity<AbuseUserReport>(entity =>
		{
			entity.Property(e => e.CreatedAt).HasComment("The created date of the AbuseUserReport.");
			entity.Property(e => e.Forwarded).HasDefaultValue(false);
			entity.Property(e => e.ReporterHost).HasComment("[Denormalized]");
			entity.Property(e => e.Resolved).HasDefaultValue(false);
			entity.Property(e => e.TargetUserHost).HasComment("[Denormalized]");

			entity.HasOne(d => d.Assignee)
			      .WithMany(p => p.AbuseUserReportAssignees)
			      .OnDelete(DeleteBehavior.SetNull);

			entity.HasOne(d => d.Reporter)
			      .WithMany(p => p.AbuseUserReportReporters)
			      .OnDelete(DeleteBehavior.Cascade);

			entity.HasOne(d => d.TargetUser)
			      .WithMany(p => p.AbuseUserReportTargetUsers)
			      .OnDelete(DeleteBehavior.Cascade);
		});

		modelBuilder.Entity<Announcement>(entity =>
		{
			entity.Property(e => e.CreatedAt).HasComment("The created date of the Announcement.");
			entity.Property(e => e.IsGoodNews).HasDefaultValue(false);
			entity.Property(e => e.ShowPopup).HasDefaultValue(false);
			entity.Property(e => e.UpdatedAt).HasComment("The updated date of the Announcement.");
		});

		modelBuilder.Entity<AnnouncementRead>(entity =>
		{
			entity.Property(e => e.CreatedAt).HasComment("The created date of the AnnouncementRead.");

			entity.HasOne(d => d.Announcement)
			      .WithMany(p => p.AnnouncementReads)
			      .OnDelete(DeleteBehavior.Cascade);

			entity.HasOne(d => d.User)
			      .WithMany(p => p.AnnouncementReads)
			      .OnDelete(DeleteBehavior.Cascade);
		});

		modelBuilder.Entity<Antenna>(entity =>
		{
			entity.Property(e => e.CaseSensitive).HasDefaultValue(false);
			entity.Property(e => e.CreatedAt).HasComment("The created date of the Antenna.");
			entity.Property(e => e.ExcludeKeywords).HasDefaultValueSql("'[]'::jsonb");
			entity.Property(e => e.Instances).HasDefaultValueSql("'[]'::jsonb");
			entity.Property(e => e.Keywords).HasDefaultValueSql("'[]'::jsonb");
			entity.Property(e => e.Name).HasComment("The name of the Antenna.");
			entity.Property(e => e.UserId).HasComment("The owner ID.");
			entity.Property(e => e.Users).HasDefaultValueSql("'{}'::character varying[]");
			entity.Property(e => e.WithReplies).HasDefaultValue(false);

			entity.HasOne(d => d.UserGroupMember)
			      .WithMany(p => p.Antennas)
			      .OnDelete(DeleteBehavior.Cascade);

			entity.HasOne(d => d.User)
			      .WithMany(p => p.Antennas)
			      .OnDelete(DeleteBehavior.Cascade);

			entity.HasOne(d => d.UserList)
			      .WithMany(p => p.Antennas)
			      .OnDelete(DeleteBehavior.Cascade);
		});

		modelBuilder.Entity<AttestationChallenge>(entity =>
		{
			entity.Property(e => e.Challenge).HasComment("Hex-encoded sha256 hash of the challenge.");
			entity.Property(e => e.CreatedAt).HasComment("The date challenge was created for expiry purposes.");
			entity.Property(e => e.RegistrationChallenge)
			      .HasDefaultValue(false)
			      .HasComment("Indicates that the challenge is only for registration purposes if true to prevent the challenge for being used as authentication.");

			entity.HasOne(d => d.User)
			      .WithMany(p => p.AttestationChallenges)
			      .OnDelete(DeleteBehavior.Cascade);
		});

		modelBuilder.Entity<Bite>(entity =>
		{
			entity.HasOne(d => d.User).WithMany().OnDelete(DeleteBehavior.Cascade);
			entity.HasOne(d => d.TargetUser).WithMany().OnDelete(DeleteBehavior.Cascade);
			entity.HasOne(d => d.TargetNote).WithMany().OnDelete(DeleteBehavior.Cascade);
			entity.HasOne(d => d.TargetBite).WithMany().OnDelete(DeleteBehavior.Cascade);
		});

		modelBuilder.Entity<Blocking>(entity =>
		{
			entity.Property(e => e.BlockeeId).HasComment("The blockee user ID.");
			entity.Property(e => e.BlockerId).HasComment("The blocker user ID.");
			entity.Property(e => e.CreatedAt).HasComment("The created date of the Blocking.");

			entity.HasOne(d => d.Blockee)
			      .WithMany(p => p.IncomingBlocks)
			      .OnDelete(DeleteBehavior.Cascade);

			entity.HasOne(d => d.Blocker)
			      .WithMany(p => p.OutgoingBlocks)
			      .OnDelete(DeleteBehavior.Cascade);
		});

		modelBuilder.Entity<Channel>(entity =>
		{
			entity.Property(e => e.BannerId).HasComment("The ID of banner Channel.");
			entity.Property(e => e.CreatedAt).HasComment("The created date of the Channel.");
			entity.Property(e => e.Description).HasComment("The description of the Channel.");
			entity.Property(e => e.Name).HasComment("The name of the Channel.");
			entity.Property(e => e.NotesCount)
			      .HasDefaultValue(0)
			      .HasComment("The count of notes.");
			entity.Property(e => e.UserId).HasComment("The owner ID.");
			entity.Property(e => e.UsersCount)
			      .HasDefaultValue(0)
			      .HasComment("The count of users.");

			entity.HasOne(d => d.Banner)
			      .WithMany(p => p.Channels)
			      .OnDelete(DeleteBehavior.SetNull);

			entity.HasOne(d => d.User)
			      .WithMany(p => p.Channels)
			      .OnDelete(DeleteBehavior.SetNull);
		});

		modelBuilder.Entity<ChannelFollowing>(entity =>
		{
			entity.Property(e => e.CreatedAt).HasComment("The created date of the ChannelFollowing.");
			entity.Property(e => e.FolloweeId).HasComment("The followee channel ID.");
			entity.Property(e => e.FollowerId).HasComment("The follower user ID.");

			entity.HasOne(d => d.Followee)
			      .WithMany(p => p.ChannelFollowings)
			      .OnDelete(DeleteBehavior.Cascade);

			entity.HasOne(d => d.Follower)
			      .WithMany(p => p.ChannelFollowings)
			      .OnDelete(DeleteBehavior.Cascade);
		});

		modelBuilder.Entity<ChannelNotePin>(entity =>
		{
			entity.Property(e => e.CreatedAt).HasComment("The created date of the ChannelNotePin.");

			entity.HasOne(d => d.Channel)
			      .WithMany(p => p.ChannelNotePins)
			      .OnDelete(DeleteBehavior.Cascade);

			entity.HasOne(d => d.Note)
			      .WithMany(p => p.ChannelNotePins)
			      .OnDelete(DeleteBehavior.Cascade);
		});

		modelBuilder.Entity<Clip>(entity =>
		{
			entity.Property(e => e.CreatedAt).HasComment("The created date of the Clip.");
			entity.Property(e => e.Description).HasComment("The description of the Clip.");
			entity.Property(e => e.IsPublic).HasDefaultValue(false);
			entity.Property(e => e.Name).HasComment("The name of the Clip.");
			entity.Property(e => e.UserId).HasComment("The owner ID.");

			entity.HasOne(d => d.User)
			      .WithMany(p => p.Clips)
			      .OnDelete(DeleteBehavior.Cascade);
		});

		modelBuilder.Entity<ClipNote>(entity =>
		{
			entity.Property(e => e.ClipId).HasComment("The clip ID.");
			entity.Property(e => e.NoteId).HasComment("The note ID.");

			entity.HasOne(d => d.Clip)
			      .WithMany(p => p.ClipNotes)
			      .OnDelete(DeleteBehavior.Cascade);

			entity.HasOne(d => d.Note)
			      .WithMany(p => p.ClipNotes)
			      .OnDelete(DeleteBehavior.Cascade);
		});

		modelBuilder.Entity<DriveFile>(entity =>
		{
			entity.Property(e => e.Blurhash).HasComment("The BlurHash string.");
			entity.Property(e => e.Comment).HasComment("The comment of the DriveFile.");
			entity.Property(e => e.CreatedAt).HasComment("The created date of the DriveFile.");
			entity.Property(e => e.FolderId)
			      .HasComment("The parent folder ID. If null, it means the DriveFile is located in root.");
			entity.Property(e => e.IsLink)
			      .HasDefaultValue(false)
			      .HasComment("Whether the DriveFile is direct link to remote server.");
			entity.Property(e => e.IsSensitive)
			      .HasDefaultValue(false)
			      .HasComment("Whether the DriveFile is NSFW.");
			entity.Property(e => e.Sha256).HasComment("The SHA256 hash of the DriveFile.");
			entity.Property(e => e.Name).HasComment("The file name of the DriveFile.");
			entity.Property(e => e.Properties)
			      .HasDefaultValueSql("'{}'::jsonb")
			      .HasComment("The any properties of the DriveFile. For example, it includes image width/height.");
			entity.Property(e => e.RequestHeaders).HasDefaultValueSql("'{}'::jsonb");
			entity.Property(e => e.Size).HasComment("The file size (bytes) of the DriveFile.");
			entity.Property(e => e.ThumbnailUrl).HasComment("The URL of the thumbnail of the DriveFile.");
			entity.Property(e => e.Type).HasComment("The content type (MIME) of the DriveFile.");
			entity.Property(e => e.Uri)
			      .HasComment("The URI of the DriveFile. it will be null when the DriveFile is local.");
			entity.Property(e => e.Url).HasComment("The URL of the DriveFile.");
			entity.Property(e => e.UserHost).HasComment("The host of owner. It will be null if the user in local.");
			entity.Property(e => e.UserId).HasComment("The owner ID.");
			entity.Property(e => e.WebpublicUrl).HasComment("The URL of the webpublic of the DriveFile.");

			entity.HasOne(d => d.Folder)
			      .WithMany(p => p.DriveFiles)
			      .OnDelete(DeleteBehavior.SetNull);

			entity.HasOne(d => d.User)
			      .WithMany(p => p.DriveFiles)
			      .OnDelete(DeleteBehavior.SetNull);
		});

		modelBuilder.Entity<DriveFolder>(entity =>
		{
			entity.Property(e => e.CreatedAt).HasComment("The created date of the DriveFolder.");
			entity.Property(e => e.Name).HasComment("The name of the DriveFolder.");
			entity.Property(e => e.ParentId)
			      .HasComment("The parent folder ID. If null, it means the DriveFolder is located in root.");
			entity.Property(e => e.UserId).HasComment("The owner ID.");

			entity.HasOne(d => d.Parent)
			      .WithMany(p => p.InverseParent)
			      .OnDelete(DeleteBehavior.SetNull);

			entity.HasOne(d => d.User)
			      .WithMany(p => p.DriveFolders)
			      .OnDelete(DeleteBehavior.Cascade);
		});

		modelBuilder.Entity<Emoji>(entity =>
		{
			entity.Property(e => e.Aliases).HasDefaultValueSql("'{}'::character varying[]");
			entity.Property(e => e.Height).HasComment("Image height");
			entity.Property(e => e.PublicUrl).HasDefaultValueSql("''::character varying");
			entity.Property(e => e.Width).HasComment("Image width");
		});

		modelBuilder.Entity<FollowRequest>(entity =>
		{
			entity.Property(e => e.CreatedAt).HasComment("The created date of the FollowRequest.");
			entity.Property(e => e.FolloweeHost).HasComment("[Denormalized]");
			entity.Property(e => e.FolloweeId).HasComment("The followee user ID.");
			entity.Property(e => e.FolloweeInbox).HasComment("[Denormalized]");
			entity.Property(e => e.FolloweeSharedInbox).HasComment("[Denormalized]");
			entity.Property(e => e.FollowerHost).HasComment("[Denormalized]");
			entity.Property(e => e.FollowerId).HasComment("The follower user ID.");
			entity.Property(e => e.FollowerInbox).HasComment("[Denormalized]");
			entity.Property(e => e.FollowerSharedInbox).HasComment("[Denormalized]");
			entity.Property(e => e.RequestId).HasComment("id of Follow Activity.");

			entity.HasOne(d => d.Followee)
			      .WithMany(p => p.IncomingFollowRequests)
			      .OnDelete(DeleteBehavior.Cascade);

			entity.HasOne(d => d.Follower)
			      .WithMany(p => p.OutgoingFollowRequests)
			      .OnDelete(DeleteBehavior.Cascade);
		});

		modelBuilder.Entity<Following>(entity =>
		{
			entity.Property(e => e.CreatedAt).HasComment("The created date of the Following.");
			entity.Property(e => e.FolloweeHost).HasComment("[Denormalized]");
			entity.Property(e => e.FolloweeId).HasComment("The followee user ID.");
			entity.Property(e => e.FolloweeInbox).HasComment("[Denormalized]");
			entity.Property(e => e.FolloweeSharedInbox).HasComment("[Denormalized]");
			entity.Property(e => e.FollowerHost).HasComment("[Denormalized]");
			entity.Property(e => e.FollowerId).HasComment("The follower user ID.");
			entity.Property(e => e.FollowerInbox).HasComment("[Denormalized]");
			entity.Property(e => e.FollowerSharedInbox).HasComment("[Denormalized]");

			entity.HasOne(d => d.Followee)
			      .WithMany(p => p.IncomingFollowRelationships)
			      .OnDelete(DeleteBehavior.Cascade);

			entity.HasOne(d => d.Follower)
			      .WithMany(p => p.OutgoingFollowRelationships)
			      .OnDelete(DeleteBehavior.Cascade);
		});

		modelBuilder.Entity<GalleryLike>(entity =>
		{
			entity.HasOne(d => d.Post)
			      .WithMany(p => p.GalleryLikes)
			      .OnDelete(DeleteBehavior.Cascade);

			entity.HasOne(d => d.User)
			      .WithMany(p => p.GalleryLikes)
			      .OnDelete(DeleteBehavior.Cascade);
		});

		modelBuilder.Entity<GalleryPost>(entity =>
		{
			entity.Property(e => e.CreatedAt).HasComment("The created date of the GalleryPost.");
			entity.Property(e => e.FileIds).HasDefaultValueSql("'{}'::character varying[]");
			entity.Property(e => e.IsSensitive)
			      .HasDefaultValue(false)
			      .HasComment("Whether the post is sensitive.");
			entity.Property(e => e.LikedCount).HasDefaultValue(0);
			entity.Property(e => e.Tags).HasDefaultValueSql("'{}'::character varying[]");
			entity.Property(e => e.UpdatedAt).HasComment("The updated date of the GalleryPost.");
			entity.Property(e => e.UserId).HasComment("The ID of author.");

			entity.HasOne(d => d.User)
			      .WithMany(p => p.GalleryPosts)
			      .OnDelete(DeleteBehavior.Cascade);
		});

		modelBuilder.Entity<Hashtag>();

		modelBuilder.Entity<Instance>(entity =>
		{
			entity.Property(e => e.CaughtAt).HasComment("The caught date of the Instance.");
			entity.Property(e => e.OutgoingFollows).HasDefaultValue(0);
			entity.Property(e => e.IncomingFollows).HasDefaultValue(0);
			entity.Property(e => e.Host).HasComment("The host of the Instance.");
			entity.Property(e => e.IsNotResponding).HasDefaultValue(false);
			entity.Property(e => e.IsSuspended).HasDefaultValue(false);
			entity.Property(e => e.NotesCount)
			      .HasDefaultValue(0)
			      .HasComment("The count of the notes of the Instance.");
			entity.Property(e => e.SoftwareName).HasComment("The software of the Instance.");
			entity.Property(e => e.UsersCount)
			      .HasDefaultValue(0)
			      .HasComment("The count of the users of the Instance.");
		});

		modelBuilder.Entity<MessagingMessage>(entity =>
		{
			entity.Property(e => e.CreatedAt).HasComment("The created date of the MessagingMessage.");
			entity.Property(e => e.GroupId).HasComment("The recipient group ID.");
			entity.Property(e => e.IsRead).HasDefaultValue(false);
			entity.Property(e => e.Reads).HasDefaultValueSql("'{}'::character varying[]");
			entity.Property(e => e.RecipientId).HasComment("The recipient user ID.");
			entity.Property(e => e.UserId).HasComment("The sender user ID.");

			entity.HasOne(d => d.File)
			      .WithMany(p => p.MessagingMessages)
			      .OnDelete(DeleteBehavior.Cascade);

			entity.HasOne(d => d.Group)
			      .WithMany(p => p.MessagingMessages)
			      .OnDelete(DeleteBehavior.Cascade);

			entity.HasOne(d => d.Recipient)
			      .WithMany(p => p.MessagingMessageRecipients)
			      .OnDelete(DeleteBehavior.Cascade);

			entity.HasOne(d => d.User)
			      .WithMany(p => p.MessagingMessageUsers)
			      .OnDelete(DeleteBehavior.Cascade);
		});

		modelBuilder.Entity<Marker>(entity =>
		{
			entity.Property(d => d.Version).HasDefaultValue(0);

			entity.HasOne(d => d.User)
			      .WithMany(p => p.Markers)
			      .OnDelete(DeleteBehavior.Cascade);
		});

		modelBuilder.Entity<Meta>(entity =>
		{
			entity.Property(e => e.AllowedHosts).HasDefaultValueSql("'{}'::character varying[]");
			entity.Property(e => e.BlockedHosts).HasDefaultValueSql("'{}'::character varying[]");
			entity.Property(e => e.CacheRemoteFiles).HasDefaultValue(false);
			entity.Property(e => e.CustomMotd).HasDefaultValueSql("'{}'::character varying[]");
			entity.Property(e => e.CustomSplashIcons).HasDefaultValueSql("'{}'::character varying[]");
			entity.Property(e => e.DeeplIsPro).HasDefaultValue(false);
			entity.Property(e => e.DefaultReaction).HasDefaultValueSql("'⭐'::character varying");
			entity.Property(e => e.DisableGlobalTimeline).HasDefaultValue(false);
			entity.Property(e => e.DisableLocalTimeline).HasDefaultValue(false);
			entity.Property(e => e.DisableRecommendedTimeline).HasDefaultValue(true);
			entity.Property(e => e.DisableRegistration).HasDefaultValue(false);
			entity.Property(e => e.EmailRequiredForSignup).HasDefaultValue(false);
			entity.Property(e => e.EnableActiveEmailValidation).HasDefaultValue(true);
			entity.Property(e => e.EnableDiscordIntegration).HasDefaultValue(false);
			entity.Property(e => e.EnableEmail).HasDefaultValue(false);
			entity.Property(e => e.EnableGithubIntegration).HasDefaultValue(false);
			entity.Property(e => e.EnableHcaptcha).HasDefaultValue(false);
			entity.Property(e => e.EnableIdenticonGeneration).HasDefaultValue(true);
			entity.Property(e => e.EnableIpLogging).HasDefaultValue(false);
			entity.Property(e => e.EnableRecaptcha).HasDefaultValue(false);
			entity.Property(e => e.EnableServerMachineStats).HasDefaultValue(false);
			entity.Property(e => e.ErrorImageUrl)
			      .HasDefaultValueSql("'/static-assets/badges/error.png'::character varying");
			entity.Property(e => e.ExperimentalFeatures).HasDefaultValueSql("'{}'::jsonb");
			entity.Property(e => e.FeedbackUrl)
			      .HasDefaultValueSql("'https://iceshrimp.dev/iceshrimp/iceshrimp/issues/new'::character varying");
			entity.Property(e => e.HiddenTags).HasDefaultValueSql("'{}'::character varying[]");
			entity.Property(e => e.Langs).HasDefaultValueSql("'{}'::character varying[]");
			entity.Property(e => e.LocalDriveCapacityMb)
			      .HasDefaultValue(1024)
			      .HasComment("Drive capacity of a local user (MB)");
			entity.Property(e => e.MascotImageUrl)
			      .HasDefaultValueSql("'/static-assets/badges/info.png'::character varying");
			entity.Property(e => e.ObjectStorageS3ForcePathStyle).HasDefaultValue(true);
			entity.Property(e => e.ObjectStorageSetPublicRead).HasDefaultValue(false);
			entity.Property(e => e.ObjectStorageUseProxy).HasDefaultValue(true);
			entity.Property(e => e.ObjectStorageUseSsl).HasDefaultValue(true);
			entity.Property(e => e.PinnedPages)
			      .HasDefaultValueSql("'{/featured,/channels,/explore,/pages,/about-iceshrimp}'::character varying[]");
			entity.Property(e => e.PinnedUsers).HasDefaultValueSql("'{}'::character varying[]");
			entity.Property(e => e.PrivateMode).HasDefaultValue(false);
			entity.Property(e => e.RecommendedInstances).HasDefaultValueSql("'{}'::character varying[]");
			entity.Property(e => e.RemoteDriveCapacityMb)
			      .HasDefaultValue(32)
			      .HasComment("Drive capacity of a remote user (MB)");
			entity.Property(e => e.RepositoryUrl)
			      .HasDefaultValueSql("'https://iceshrimp.dev/iceshrimp/iceshrimp'::character varying");
			entity.Property(e => e.SecureMode).HasDefaultValue(true);
			entity.Property(e => e.SilencedHosts).HasDefaultValueSql("'{}'::character varying[]");
			entity.Property(e => e.SmtpSecure).HasDefaultValue(false);
			entity.Property(e => e.UseObjectStorage).HasDefaultValue(false);
		});

		modelBuilder.Entity<ModerationLog>(entity =>
		{
			entity.Property(e => e.CreatedAt).HasComment("The created date of the ModerationLog.");

			entity.HasOne(d => d.User)
			      .WithMany(p => p.ModerationLogs)
			      .OnDelete(DeleteBehavior.Cascade);
		});

		modelBuilder.Entity<Muting>(entity =>
		{
			entity.Property(e => e.CreatedAt).HasComment("The created date of the Muting.");
			entity.Property(e => e.MuteeId).HasComment("The mutee user ID.");
			entity.Property(e => e.MuterId).HasComment("The muter user ID.");

			entity.HasOne(d => d.Mutee)
			      .WithMany(p => p.IncomingMutes)
			      .OnDelete(DeleteBehavior.Cascade);

			entity.HasOne(d => d.Muter)
			      .WithMany(p => p.OutgoingMutes)
			      .OnDelete(DeleteBehavior.Cascade);
		});

		modelBuilder.Entity<Note>(entity =>
		{
			entity.HasIndex(e => e.Mentions, "GIN_note_mentions").HasMethod("gin");
			entity.HasIndex(e => e.Tags, "GIN_note_tags").HasMethod("gin");
			entity.HasIndex(e => e.VisibleUserIds, "GIN_note_visibleUserIds").HasMethod("gin");
			entity.HasIndex(e => e.Text, "GIN_TRGM_note_text")
			      .HasMethod("gin")
			      .HasOperators("gin_trgm_ops");
			entity.HasIndex(e => e.Cw, "GIN_TRGM_note_cw")
			      .HasMethod("gin")
			      .HasOperators("gin_trgm_ops");

			entity.Property(e => e.AttachedFileTypes).HasDefaultValueSql("'{}'::character varying[]");
			entity.Property(e => e.ChannelId).HasComment("The ID of source channel.");
			entity.Property(e => e.CreatedAt).HasComment("The created date of the Note.");
			entity.Property(e => e.Emojis).HasDefaultValueSql("'{}'::character varying[]");
			entity.Property(e => e.FileIds).HasDefaultValueSql("'{}'::character varying[]");
			entity.Property(e => e.HasPoll).HasDefaultValue(false);
			entity.Property(e => e.LikeCount).HasDefaultValue(0);
			entity.Property(e => e.LocalOnly).HasDefaultValue(false);
			entity.Property(e => e.MentionedRemoteUsers).HasDefaultValueSql("'[]'::jsonb");
			entity.Property(e => e.Mentions).HasDefaultValueSql("'{}'::character varying[]");
			entity.Property(e => e.Reactions).HasDefaultValueSql("'{}'::jsonb");
			entity.Property(e => e.RenoteCount).HasDefaultValue((short)0);
			entity.Property(e => e.RenoteId).HasComment("The ID of renote target.");
			entity.Property(e => e.RenoteUserHost).HasComment("[Denormalized]");
			entity.Property(e => e.RenoteUserId).HasComment("[Denormalized]");
			entity.Property(e => e.RepliesCount).HasDefaultValue((short)0);
			entity.Property(e => e.ReplyId).HasComment("The ID of reply target.");
			entity.Property(e => e.ReplyUserHost).HasComment("[Denormalized]");
			entity.Property(e => e.ReplyUserId).HasComment("[Denormalized]");
			entity.Property(e => e.Score).HasDefaultValue(0);
			entity.Property(e => e.Tags).HasDefaultValueSql("'{}'::character varying[]");
			entity.Property(e => e.UpdatedAt).HasComment("The updated date of the Note.");
			entity.Property(e => e.Uri).HasComment("The URI of a note. it will be null when the note is local.");
			entity.Property(e => e.Url)
			      .HasComment("The human readable url of a note. it will be null when the note is local.");
			entity.Property(e => e.UserHost).HasComment("[Denormalized]");
			entity.Property(e => e.UserId).HasComment("The ID of author.");
			entity.Property(e => e.VisibleUserIds).HasDefaultValueSql("'{}'::character varying[]");

			entity.HasOne(d => d.Channel)
			      .WithMany(p => p.Notes)
			      .OnDelete(DeleteBehavior.Cascade);

			entity.HasOne(d => d.Renote)
			      .WithMany(p => p.InverseRenote)
			      .OnDelete(DeleteBehavior.Cascade);

			entity.HasOne(d => d.Reply)
			      .WithMany(p => p.InverseReply)
			      .OnDelete(DeleteBehavior.Cascade);

			entity.HasOne(d => d.User)
			      .WithMany(p => p.Notes)
			      .OnDelete(DeleteBehavior.Cascade);
		});

		modelBuilder.Entity<NoteEdit>(entity =>
		{
			entity.Property(e => e.FileIds).HasDefaultValueSql("'{}'::character varying[]");
			entity.Property(e => e.NoteId).HasComment("The ID of note.");
			entity.Property(e => e.UpdatedAt).HasComment("The updated date of the Note.");

			entity.HasOne(d => d.Note)
			      .WithMany(p => p.NoteEdits)
			      .OnDelete(DeleteBehavior.Cascade);
		});

		modelBuilder.Entity<NoteBookmark>(entity =>
		{
			entity.Property(e => e.CreatedAt).HasComment("The created date of the NoteBookmark.");

			entity.HasOne(d => d.Note)
			      .WithMany(p => p.NoteBookmarks)
			      .OnDelete(DeleteBehavior.Cascade);

			entity.HasOne(d => d.User)
			      .WithMany(p => p.NoteBookmarks)
			      .OnDelete(DeleteBehavior.Cascade);
		});

		modelBuilder.Entity<NoteLike>(entity =>
		{
			entity.HasOne(d => d.Note)
			      .WithMany(p => p.NoteLikes)
			      .OnDelete(DeleteBehavior.Cascade);

			entity.HasOne(d => d.User)
			      .WithMany(p => p.NoteLikes)
			      .OnDelete(DeleteBehavior.Cascade);
		});

		modelBuilder.Entity<NoteReaction>(entity =>
		{
			entity.Property(e => e.CreatedAt).HasComment("The created date of the NoteReaction.");

			entity.HasOne(d => d.Note)
			      .WithMany(p => p.NoteReactions)
			      .OnDelete(DeleteBehavior.Cascade);

			entity.HasOne(d => d.User)
			      .WithMany(p => p.NoteReactions)
			      .OnDelete(DeleteBehavior.Cascade);
		});

		modelBuilder.Entity<NoteThreadMuting>(entity =>
		{
			entity.HasOne(d => d.User)
			      .WithMany(p => p.NoteThreadMutings)
			      .OnDelete(DeleteBehavior.Cascade);
		});

		modelBuilder.Entity<NoteUnread>(entity =>
		{
			entity.Property(e => e.NoteChannelId).HasComment("[Denormalized]");
			entity.Property(e => e.NoteUserId).HasComment("[Denormalized]");

			entity.HasOne(d => d.Note)
			      .WithMany(p => p.NoteUnreads)
			      .OnDelete(DeleteBehavior.Cascade);

			entity.HasOne(d => d.User)
			      .WithMany(p => p.NoteUnreads)
			      .OnDelete(DeleteBehavior.Cascade);
		});

		modelBuilder.Entity<NoteWatching>(entity =>
		{
			entity.Property(e => e.CreatedAt).HasComment("The created date of the NoteWatching.");
			entity.Property(e => e.NoteId).HasComment("The target Note ID.");
			entity.Property(e => e.NoteUserId).HasComment("[Denormalized]");
			entity.Property(e => e.UserId).HasComment("The watcher ID.");

			entity.HasOne(d => d.Note)
			      .WithMany(p => p.NoteWatchings)
			      .OnDelete(DeleteBehavior.Cascade);

			entity.HasOne(d => d.User)
			      .WithMany(p => p.NoteWatchings)
			      .OnDelete(DeleteBehavior.Cascade);
		});

		modelBuilder.Entity<Notification>(entity =>
		{
			entity.Property(e => e.CreatedAt).HasComment("The created date of the Notification.");
			entity.Property(e => e.IsRead)
			      .HasDefaultValue(false)
			      .HasComment("Whether the notification was read.");
			entity.Property(e => e.NotifieeId).HasComment("The ID of recipient user of the Notification.");
			entity.Property(e => e.NotifierId).HasComment("The ID of sender user of the Notification.");
			entity.Property(e => e.Type).HasComment("The type of the Notification.");

			entity.HasOne(d => d.FollowRequest)
			      .WithMany(p => p.Notifications)
			      .OnDelete(DeleteBehavior.Cascade);

			entity.HasOne(d => d.Note)
			      .WithMany(p => p.Notifications)
			      .OnDelete(DeleteBehavior.Cascade);

			entity.HasOne(d => d.Notifiee)
			      .WithMany(p => p.NotificationNotifiees)
			      .OnDelete(DeleteBehavior.Cascade);

			entity.HasOne(d => d.Notifier)
			      .WithMany(p => p.NotificationNotifiers)
			      .OnDelete(DeleteBehavior.Cascade);

			entity.HasOne(d => d.UserGroupInvitation)
			      .WithMany(p => p.Notifications)
			      .OnDelete(DeleteBehavior.Cascade);
		});

		modelBuilder.Entity<OauthApp>(entity =>
		{
			entity.Property(e => e.ClientId).HasComment("The client id of the OAuth application");
			entity.Property(e => e.ClientSecret).HasComment("The client secret of the OAuth application");
			entity.Property(e => e.CreatedAt).HasComment("The created date of the OAuth application");
			entity.Property(e => e.Name).HasComment("The name of the OAuth application");
			entity.Property(e => e.RedirectUris).HasComment("The redirect URIs of the OAuth application");
			entity.Property(e => e.Scopes).HasComment("The scopes requested by the OAuth application");
			entity.Property(e => e.Website).HasComment("The website of the OAuth application");
		});

		modelBuilder.Entity<OauthToken>(entity =>
		{
			entity.Property(e => e.Active).HasComment("Whether or not the token has been activated");
			entity.Property(e => e.Code).HasComment("The auth code for the OAuth token");
			entity.Property(e => e.CreatedAt).HasComment("The created date of the OAuth token");
			entity.Property(e => e.RedirectUri).HasComment("The redirect URI of the OAuth token");
			entity.Property(e => e.Scopes).HasComment("The scopes requested by the OAuth token");
			entity.Property(e => e.Token).HasComment("The OAuth token");
			entity.Property(e => e.SupportsHtmlFormatting)
			      .HasComment("Whether the client supports HTML inline formatting (bold, italic, strikethrough, ...)")
			      .HasDefaultValue(true);
			entity.Property(e => e.AutoDetectQuotes)
			      .HasComment("Whether the backend should automatically detect quote posts coming from this client")
			      .HasDefaultValue(true);

			entity.HasOne(d => d.App)
			      .WithMany(p => p.OauthTokens)
			      .OnDelete(DeleteBehavior.Cascade);

			entity.HasOne(d => d.User)
			      .WithMany(p => p.OauthTokens)
			      .OnDelete(DeleteBehavior.Cascade);
		});

		modelBuilder.Entity<Page>(entity =>
		{
			entity.Property(e => e.Content).HasDefaultValueSql("'[]'::jsonb");
			entity.Property(e => e.CreatedAt).HasComment("The created date of the Page.");
			entity.Property(e => e.HideTitleWhenPinned).HasDefaultValue(false);
			entity.Property(e => e.LikedCount).HasDefaultValue(0);
			entity.Property(e => e.Script).HasDefaultValueSql("''::character varying");
			entity.Property(e => e.UpdatedAt).HasComment("The updated date of the Page.");
			entity.Property(e => e.UserId).HasComment("The ID of author.");
			entity.Property(e => e.Variables).HasDefaultValueSql("'[]'::jsonb");
			entity.Property(e => e.VisibleUserIds).HasDefaultValueSql("'{}'::character varying[]");

			entity.HasOne(d => d.EyeCatchingImage)
			      .WithMany(p => p.Pages)
			      .OnDelete(DeleteBehavior.Cascade);

			entity.HasOne(d => d.User)
			      .WithMany(p => p.Pages)
			      .OnDelete(DeleteBehavior.Cascade);
		});

		modelBuilder.Entity<PageLike>(entity =>
		{
			entity.HasOne(d => d.Page)
			      .WithMany(p => p.PageLikes)
			      .OnDelete(DeleteBehavior.Cascade);

			entity.HasOne(d => d.User)
			      .WithMany(p => p.PageLikes)
			      .OnDelete(DeleteBehavior.Cascade);
		});

		modelBuilder.Entity<PasswordResetRequest>(entity =>
		{
			entity.HasOne(d => d.User)
			      .WithMany(p => p.PasswordResetRequests)
			      .OnDelete(DeleteBehavior.Cascade);
		});

		modelBuilder.Entity<Poll>(entity =>
		{
			entity.Property(e => e.Choices).HasDefaultValueSql("'{}'::character varying[]");
			entity.Property(e => e.UserHost).HasComment("[Denormalized]");
			entity.Property(e => e.UserId).HasComment("[Denormalized]");
			entity.Property(e => e.NoteVisibility).HasComment("[Denormalized]");

			entity.HasOne(d => d.Note)
			      .WithOne(p => p.Poll)
			      .OnDelete(DeleteBehavior.Cascade);
		});

		modelBuilder.Entity<PollVote>(entity =>
		{
			entity.Property(e => e.CreatedAt).HasComment("The created date of the PollVote.");

			entity.HasOne(d => d.Note)
			      .WithMany(p => p.PollVotes)
			      .OnDelete(DeleteBehavior.Cascade);

			entity.HasOne(d => d.User)
			      .WithMany(p => p.PollVotes)
			      .OnDelete(DeleteBehavior.Cascade);
		});

		modelBuilder.Entity<PromoNote>(entity =>
		{
			entity.Property(e => e.UserId).HasComment("[Denormalized]");

			entity.HasOne(d => d.Note)
			      .WithOne(p => p.PromoNote)
			      .OnDelete(DeleteBehavior.Cascade);
		});

		modelBuilder.Entity<PromoRead>(entity =>
		{
			entity.Property(e => e.CreatedAt).HasComment("The created date of the PromoRead.");

			entity.HasOne(d => d.Note)
			      .WithMany(p => p.PromoReads)
			      .OnDelete(DeleteBehavior.Cascade);

			entity.HasOne(d => d.User)
			      .WithMany(p => p.PromoReads)
			      .OnDelete(DeleteBehavior.Cascade);
		});

		modelBuilder.Entity<RegistrationInvite>();

		modelBuilder.Entity<RegistryItem>(entity =>
		{
			entity.Property(e => e.CreatedAt).HasComment("The created date of the RegistryItem.");
			entity.Property(e => e.Key).HasComment("The key of the RegistryItem.");
			entity.Property(e => e.Scope).HasDefaultValueSql("'{}'::character varying[]");
			entity.Property(e => e.UpdatedAt).HasComment("The updated date of the RegistryItem.");
			entity.Property(e => e.UserId).HasComment("The owner ID.");
			entity.Property(e => e.Value)
			      .HasDefaultValueSql("'{}'::jsonb")
			      .HasComment("The value of the RegistryItem.");

			entity.HasOne(d => d.User)
			      .WithMany(p => p.RegistryItems)
			      .OnDelete(DeleteBehavior.Cascade);
		});

		modelBuilder.Entity<Relay>(_ => { });

		modelBuilder.Entity<RenoteMuting>(entity =>
		{
			entity.Property(e => e.CreatedAt).HasComment("The created date of the Muting.");
			entity.Property(e => e.MuteeId).HasComment("The mutee user ID.");
			entity.Property(e => e.MuterId).HasComment("The muter user ID.");

			entity.HasOne(d => d.Mutee)
			      .WithMany(p => p.RenoteMutingMutees)
			      .OnDelete(DeleteBehavior.Cascade);

			entity.HasOne(d => d.Muter)
			      .WithMany(p => p.RenoteMutingMuters)
			      .OnDelete(DeleteBehavior.Cascade);
		});

		modelBuilder.Entity<Session>(entity =>
		{
			entity.Property(e => e.Active)
			      .HasComment("Whether or not the token has been activated (i.e. 2fa has been confirmed)");
			entity.Property(e => e.CreatedAt).HasComment("The created date of the OAuth token");
			entity.Property(e => e.Token).HasComment("The authorization token");

			entity.HasOne(d => d.User)
			      .WithMany(p => p.Sessions)
			      .OnDelete(DeleteBehavior.Cascade);
		});

		modelBuilder.Entity<SwSubscription>(entity =>
		{
			entity.Property(e => e.SendReadMessage).HasDefaultValue(false);

			entity.HasOne(d => d.User)
			      .WithMany(p => p.SwSubscriptions)
			      .OnDelete(DeleteBehavior.Cascade);
		});

		modelBuilder.Entity<PushSubscription>(entity =>
		{
			entity.Property(e => e.Types).HasDefaultValueSql("'{}'::character varying[]");
			entity.Property(e => e.Policy).HasDefaultValue(PushSubscription.PushPolicy.All);

			entity.HasOne(d => d.User)
			      .WithMany(p => p.PushSubscriptions)
			      .OnDelete(DeleteBehavior.Cascade);

			entity.HasOne(d => d.OauthToken)
			      .WithOne(p => p.PushSubscription)
			      .OnDelete(DeleteBehavior.Cascade);
		});

		modelBuilder.Entity<UsedUsername>();

		modelBuilder.Entity<User>(entity =>
		{
			entity.Property(e => e.AlsoKnownAs).HasComment("URIs the user is known as too");
			entity.Property(e => e.AvatarBlurhash).HasComment("The blurhash of the avatar DriveFile");
			entity.Property(e => e.AvatarId).HasComment("The ID of avatar DriveFile.");
			entity.Property(e => e.AvatarUrl).HasComment("The URL of the avatar DriveFile");
			entity.Property(e => e.BannerBlurhash).HasComment("The blurhash of the banner DriveFile");
			entity.Property(e => e.BannerId).HasComment("The ID of banner DriveFile.");
			entity.Property(e => e.BannerUrl).HasComment("The URL of the banner DriveFile");
			entity.Property(e => e.CreatedAt).HasComment("The created date of the User.");
			entity.Property(e => e.DriveCapacityOverrideMb).HasComment("Overrides user drive capacity limit");
			entity.Property(e => e.Emojis).HasDefaultValueSql("'{}'::character varying[]");
			entity.Property(e => e.Featured)
			      .HasComment("The featured URL of the User. It will be null if the origin of the user is local.");
			entity.Property(e => e.FollowersCount)
			      .HasDefaultValue(0)
			      .HasComment("The count of followers.");
			entity.Property(e => e.FollowersUri)
			      .HasComment("The URI of the user Follower Collection. It will be null if the origin of the user is local.");
			entity.Property(e => e.FollowingCount)
			      .HasDefaultValue(0)
			      .HasComment("The count of following.");
			entity.Property(e => e.HideOnlineStatus).HasDefaultValue(false);
			entity.Property(e => e.Host)
			      .HasComment("The host of the User. It will be null if the origin of the user is local.");
			entity.Property(e => e.Inbox)
			      .HasComment("The inbox URL of the User. It will be null if the origin of the user is local.");
			entity.Property(e => e.IsAdmin)
			      .HasDefaultValue(false)
			      .HasComment("Whether the User is the admin.");
			entity.Property(e => e.IsBot)
			      .HasDefaultValue(false)
			      .HasComment("Whether the User is a bot.");
			entity.Property(e => e.IsCat)
			      .HasDefaultValue(false)
			      .HasComment("Whether the User is a cat.");
			entity.Property(e => e.IsDeleted)
			      .HasDefaultValue(false)
			      .HasComment("Whether the User is deleted.");
			entity.Property(e => e.IsExplorable)
			      .HasDefaultValue(true)
			      .HasComment("Whether the User is explorable.");
			entity.Property(e => e.IsLocked)
			      .HasDefaultValue(false)
			      .HasComment("Whether the User is locked.");
			entity.Property(e => e.IsModerator)
			      .HasDefaultValue(false)
			      .HasComment("Whether the User is a moderator.");
			entity.Property(e => e.IsSilenced)
			      .HasDefaultValue(false)
			      .HasComment("Whether the User is silenced.");
			entity.Property(e => e.IsSuspended)
			      .HasDefaultValue(false)
			      .HasComment("Whether the User is suspended.");
			entity.Property(e => e.MovedToUri).HasComment("The URI of the new account of the User");
			entity.Property(e => e.DisplayName).HasComment("The name of the User.");
			entity.Property(e => e.NotesCount)
			      .HasDefaultValue(0)
			      .HasComment("The count of notes.");
			entity.Property(e => e.SharedInbox)
			      .HasComment("The sharedInbox URL of the User. It will be null if the origin of the user is local.");
			entity.Property(e => e.SpeakAsCat)
			      .HasDefaultValue(true)
			      .HasComment("Whether to speak as a cat if isCat.");
			entity.Property(e => e.Tags).HasDefaultValueSql("'{}'::character varying[]");
			entity.Property(e => e.Token)
			      .IsFixedLength()
			      .HasComment("The native access token of the User. It will be null if the origin of the user is local.");
			entity.Property(e => e.UpdatedAt).HasComment("The updated date of the User.");
			entity.Property(e => e.Uri)
			      .HasComment("The URI of the User. It will be null if the origin of the user is local.");
			entity.Property(e => e.Username).HasComment("The username of the User.");
			entity.Property(e => e.UsernameLower).HasComment("The username (lowercased) of the User.");

			entity.HasOne(d => d.Avatar)
			      .WithOne(p => p.UserAvatar)
			      .OnDelete(DeleteBehavior.SetNull);

			entity.HasOne(d => d.Banner)
			      .WithOne(p => p.UserBanner)
			      .OnDelete(DeleteBehavior.SetNull);
		});

		modelBuilder.Entity<UserGroup>(entity =>
		{
			entity.Property(e => e.CreatedAt).HasComment("The created date of the UserGroup.");
			entity.Property(e => e.IsPrivate).HasDefaultValue(false);
			entity.Property(e => e.UserId).HasComment("The ID of owner.");

			entity.HasOne(d => d.User)
			      .WithMany(p => p.UserGroups)
			      .OnDelete(DeleteBehavior.Cascade);
		});

		modelBuilder.Entity<UserGroupInvitation>(entity =>
		{
			entity.Property(e => e.CreatedAt).HasComment("The created date of the UserGroupInvitation.");
			entity.Property(e => e.UserGroupId).HasComment("The group ID.");
			entity.Property(e => e.UserId).HasComment("The user ID.");

			entity.HasOne(d => d.UserGroup)
			      .WithMany(p => p.UserGroupInvitations)
			      .OnDelete(DeleteBehavior.Cascade);

			entity.HasOne(d => d.User)
			      .WithMany(p => p.UserGroupInvitations)
			      .OnDelete(DeleteBehavior.Cascade);
		});

		modelBuilder.Entity<UserGroupMember>(entity =>
		{
			entity.Property(e => e.CreatedAt).HasComment("The created date of the UserGroupMember.");
			entity.Property(e => e.UserGroupId).HasComment("The group ID.");
			entity.Property(e => e.UserId).HasComment("The user ID.");

			entity.HasOne(d => d.UserGroup)
			      .WithMany(p => p.UserGroupMembers)
			      .OnDelete(DeleteBehavior.Cascade);

			entity.HasOne(d => d.User)
			      .WithMany(p => p.UserGroupMemberships)
			      .OnDelete(DeleteBehavior.Cascade);
		});

		modelBuilder.Entity<UserKeypair>(entity =>
		{
			entity.HasOne(d => d.User)
			      .WithOne(p => p.UserKeypair)
			      .OnDelete(DeleteBehavior.Cascade);
		});

		modelBuilder.Entity<UserList>(entity =>
		{
			entity.Property(e => e.CreatedAt).HasComment("The created date of the UserList.");
			entity.Property(e => e.HideFromHomeTl)
			      .HasDefaultValue(false)
			      .HasComment("Whether posts from list members should be hidden from the home timeline.");
			entity.Property(e => e.Name).HasComment("The name of the UserList.");
			entity.Property(e => e.UserId).HasComment("The owner ID.");

			entity.HasOne(d => d.User)
			      .WithMany(p => p.UserLists)
			      .OnDelete(DeleteBehavior.Cascade);
		});

		modelBuilder.Entity<UserListMember>(entity =>
		{
			entity.Property(e => e.CreatedAt).HasComment("The created date of the UserListMember.");
			entity.Property(e => e.UserId).HasComment("The user ID.");
			entity.Property(e => e.UserListId).HasComment("The list ID.");

			entity.HasOne(d => d.User)
			      .WithMany(p => p.UserListMembers)
			      .OnDelete(DeleteBehavior.Cascade);

			entity.HasOne(d => d.UserList)
			      .WithMany(p => p.UserListMembers)
			      .OnDelete(DeleteBehavior.Cascade);
		});

		modelBuilder.Entity<UserNotePin>(entity =>
		{
			entity.Property(e => e.CreatedAt).HasComment("The created date of the UserNotePins.");

			entity.HasOne(d => d.Note)
			      .WithMany(p => p.UserNotePins)
			      .OnDelete(DeleteBehavior.Cascade);

			entity.HasOne(d => d.User)
			      .WithMany(p => p.UserNotePins)
			      .OnDelete(DeleteBehavior.Cascade);
		});

		modelBuilder.Entity<UserPending>();

		modelBuilder.Entity<UserProfile>(entity =>
		{
			entity.Property(e => e.AlwaysMarkNsfw).HasDefaultValue(false);
			entity.Property(e => e.AutoAcceptFollowed).HasDefaultValue(false);
			entity.Property(e => e.Birthday)
			      .IsFixedLength()
			      .HasComment("The birthday (YYYY-MM-DD) of the User.");
			entity.Property(e => e.CarefulBot).HasDefaultValue(false);
			entity.Property(e => e.ClientData)
			      .HasDefaultValueSql("'{}'::jsonb")
			      .HasComment("The client-specific data of the User.");
			entity.Property(e => e.Description).HasComment("The description (bio) of the User.");
			entity.Property(e => e.Email).HasComment("The email address of the User.");
			entity.Property(e => e.EmailNotificationTypes)
			      .HasDefaultValueSql("'[\"follow\", \"receiveFollowRequest\", \"groupInvited\"]'::jsonb");
			entity.Property(e => e.EmailVerified).HasDefaultValue(false);
			entity.Property(e => e.EnableWordMute).HasDefaultValue(false);
			entity.Property(e => e.Fields).HasDefaultValueSql("'[]'::jsonb");
			entity.Property(e => e.InjectFeaturedNote).HasDefaultValue(true);
			entity.Property(e => e.Integrations).HasDefaultValueSql("'{}'::jsonb");
			entity.Property(e => e.Location).HasComment("The location of the User.");
			entity.Property(e => e.Mentions).HasDefaultValueSql("'[]'::jsonb");
			entity.Property(e => e.ModerationNote).HasDefaultValueSql("''::character varying");
			entity.Property(e => e.MutedInstances)
			      .HasDefaultValueSql("'[]'::jsonb")
			      .HasComment("List of instances muted by the user.");
			entity.Property(e => e.MutedWords).HasDefaultValueSql("'[]'::jsonb");
			entity.Property(e => e.NoCrawle)
			      .HasDefaultValue(false)
			      .HasComment("Whether reject index by crawler.");
			entity.Property(e => e.Password)
			      .HasComment("The password hash of the User. It will be null if the origin of the user is local.");
			entity.Property(e => e.PreventAiLearning).HasDefaultValue(true);
			entity.Property(e => e.PublicReactions).HasDefaultValue(false);
			entity.Property(e => e.ReceiveAnnouncementEmail).HasDefaultValue(true);
			entity.Property(e => e.Room)
			      .HasDefaultValueSql("'{}'::jsonb")
			      .HasComment("The room data of the User.");
			entity.Property(e => e.SecurityKeysAvailable).HasDefaultValue(false);
			entity.Property(e => e.TwoFactorEnabled).HasDefaultValue(false);
			entity.Property(e => e.Url).HasComment("Remote URL of the user.");
			entity.Property(e => e.UsePasswordLessLogin).HasDefaultValue(false);
			entity.Property(e => e.UserHost).HasComment("[Denormalized]");
			entity.Property(e => e.MutingNotificationTypes)
			      .HasDefaultValueSql("'{}'::public.notification_type_enum[]");
			entity.Property(e => e.FFVisibility)
			      .HasDefaultValue(UserProfile.UserProfileFFVisibility.Public);
			entity.Property(e => e.MentionsResolved).HasDefaultValue(false);

			entity.HasOne(d => d.PinnedPage)
			      .WithOne(p => p.UserProfile)
			      .OnDelete(DeleteBehavior.SetNull);

			entity.HasOne(d => d.User)
			      .WithOne(p => p.UserProfile)
			      .OnDelete(DeleteBehavior.Cascade);
		});

		modelBuilder.Entity<UserPublickey>(entity =>
		{
			entity.HasOne(d => d.User)
			      .WithOne(p => p.UserPublickey)
			      .OnDelete(DeleteBehavior.Cascade);
		});

		modelBuilder.Entity<UserSecurityKey>(entity =>
		{
			entity.Property(e => e.Id).HasComment("Variable-length id given to navigator.credentials.get()");
			entity.Property(e => e.LastUsed)
			      .HasComment("The date of the last time the UserSecurityKey was successfully validated.");
			entity.Property(e => e.Name).HasComment("User-defined name for this key");
			entity.Property(e => e.PublicKey)
			      .HasComment("Variable-length public key used to verify attestations (hex-encoded).");

			entity.HasOne(d => d.User)
			      .WithMany(p => p.UserSecurityKeys)
			      .OnDelete(DeleteBehavior.Cascade);
		});

		modelBuilder.Entity<UserSettings>(entity =>
		{
			entity.Property(e => e.PrivateMode).HasDefaultValue(false);
			entity.Property(e => e.DefaultNoteVisibility).HasDefaultValue(Note.NoteVisibility.Public);
			entity.HasOne(e => e.User).WithOne(e => e.UserSettings);
		});

		modelBuilder.Entity<Webhook>(entity =>
		{
			entity.Property(e => e.Active).HasDefaultValue(true);
			entity.Property(e => e.CreatedAt).HasComment("The created date of the Antenna.");
			entity.Property(e => e.Name).HasComment("The name of the Antenna.");
			entity.Property(e => e.On).HasDefaultValueSql("'{}'::character varying[]");
			entity.Property(e => e.UserId).HasComment("The owner ID.");

			entity.HasOne(d => d.User)
			      .WithMany(p => p.Webhooks)
			      .OnDelete(DeleteBehavior.Cascade);
		});
	}

	public async Task ReloadEntityAsync(object entity)
	{
		await Entry(entity).ReloadAsync();
	}

	public async Task ReloadEntityRecursivelyAsync(object entity)
	{
		await ReloadEntityAsync(entity);
		await Entry(entity)
		      .References.Where(p => p is { IsLoaded: true, TargetEntry: not null })
		      .Select(p => p.TargetEntry!.ReloadAsync())
		      .AwaitAllNoConcurrencyAsync();
	}

	public IQueryable<Note> NoteAncestors(string noteId, int depth)
		=> FromExpression(() => NoteAncestors(noteId, depth));

	public IQueryable<Note> NoteAncestors(Note note, int depth)
		=> FromExpression(() => NoteAncestors(note.Id, depth));

	public IQueryable<Note> NoteDescendants(string noteId, int depth, int breadth)
		=> FromExpression(() => NoteDescendants(noteId, depth, breadth));

	public IQueryable<Note> NoteDescendants(Note note, int depth, int breadth)
		=> FromExpression(() => NoteDescendants(note.Id, depth, breadth));

	public IQueryable<Note> Conversations(string userId)
		=> FromExpression(() => Conversations(userId));

	public IQueryable<Note> Conversations(User user)
		=> FromExpression(() => Conversations(user.Id));
}

[SuppressMessage("ReSharper", "UnusedType.Global",
                 Justification = "Constructed using reflection by the dotnet-ef CLI tool")]
public class DesignTimeDatabaseContextFactory : IDesignTimeDbContextFactory<DatabaseContext>
{
	DatabaseContext IDesignTimeDbContextFactory<DatabaseContext>.CreateDbContext(string[] args)
	{
		var configuration = new ConfigurationBuilder()
		                    .SetBasePath(Directory.GetCurrentDirectory())
		                    .AddCustomConfiguration()
		                    .Build();

		var config     = configuration.GetSection("Database").Get<Config.DatabaseSection>();
		var dataSource = DatabaseContext.GetDataSource(config);
		var builder    = new DbContextOptionsBuilder<DatabaseContext>();
		DatabaseContext.Configure(builder, dataSource);
		return new DatabaseContext(builder.Options);
	}
}