using System.Diagnostics.CodeAnalysis;
using Iceshrimp.Backend.Core.Database.Tables;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Iceshrimp.Backend.Core.Database;

[SuppressMessage("ReSharper", "StringLiteralTypo")]
[SuppressMessage("ReSharper", "IdentifierTypo")]
public class DatabaseContext : DbContext {
	private readonly IConfiguration? _config;

	public DatabaseContext() { }

	public DatabaseContext(DbContextOptions<DatabaseContext> options, IConfiguration? config)
		: base(options) {
		_config = config;
	}

	public virtual DbSet<AbuseUserReport>          AbuseUserReports          { get; init; } = null!;
	public virtual DbSet<AccessToken>              AccessTokens              { get; init; } = null!;
	public virtual DbSet<Announcement>             Announcements             { get; init; } = null!;
	public virtual DbSet<AnnouncementRead>         AnnouncementReads         { get; init; } = null!;
	public virtual DbSet<Antenna>                  Antennas                  { get; init; } = null!;
	public virtual DbSet<App>                      Apps                      { get; init; } = null!;
	public virtual DbSet<AttestationChallenge>     AttestationChallenges     { get; init; } = null!;
	public virtual DbSet<AuthSession>              AuthSessions              { get; init; } = null!;
	public virtual DbSet<Blocking>                 Blockings                 { get; init; } = null!;
	public virtual DbSet<Channel>                  Channels                  { get; init; } = null!;
	public virtual DbSet<ChannelFollowing>         ChannelFollowings         { get; init; } = null!;
	public virtual DbSet<ChannelNotePining>        ChannelNotePinings        { get; init; } = null!;
	public virtual DbSet<ChartActiveUser>          ChartActiveUsers          { get; init; } = null!;
	public virtual DbSet<ChartApRequest>           ChartApRequests           { get; init; } = null!;
	public virtual DbSet<ChartDayActiveUser>       ChartDayActiveUsers       { get; init; } = null!;
	public virtual DbSet<ChartDayApRequest>        ChartDayApRequests        { get; init; } = null!;
	public virtual DbSet<ChartDayDrive>            ChartDayDrives            { get; init; } = null!;
	public virtual DbSet<ChartDayFederation>       ChartDayFederations       { get; init; } = null!;
	public virtual DbSet<ChartDayHashtag>          ChartDayHashtags          { get; init; } = null!;
	public virtual DbSet<ChartDayInstance>         ChartDayInstances         { get; init; } = null!;
	public virtual DbSet<ChartDayNetwork>          ChartDayNetworks          { get; init; } = null!;
	public virtual DbSet<ChartDayNote>             ChartDayNotes             { get; init; } = null!;
	public virtual DbSet<ChartDayPerUserDrive>     ChartDayPerUserDrives     { get; init; } = null!;
	public virtual DbSet<ChartDayPerUserFollowing> ChartDayPerUserFollowings { get; init; } = null!;
	public virtual DbSet<ChartDayPerUserNote>      ChartDayPerUserNotes      { get; init; } = null!;
	public virtual DbSet<ChartDayPerUserReaction>  ChartDayPerUserReactions  { get; init; } = null!;
	public virtual DbSet<ChartDayUser>             ChartDayUsers             { get; init; } = null!;
	public virtual DbSet<ChartDrive>               ChartDrives               { get; init; } = null!;
	public virtual DbSet<ChartFederation>          ChartFederations          { get; init; } = null!;
	public virtual DbSet<ChartHashtag>             ChartHashtags             { get; init; } = null!;
	public virtual DbSet<ChartInstance>            ChartInstances            { get; init; } = null!;
	public virtual DbSet<ChartNetwork>             ChartNetworks             { get; init; } = null!;
	public virtual DbSet<ChartNote>                ChartNotes                { get; init; } = null!;
	public virtual DbSet<ChartPerUserDrive>        ChartPerUserDrives        { get; init; } = null!;
	public virtual DbSet<ChartPerUserFollowing>    ChartPerUserFollowings    { get; init; } = null!;
	public virtual DbSet<ChartPerUserNote>         ChartPerUserNotes         { get; init; } = null!;
	public virtual DbSet<ChartPerUserReaction>     ChartPerUserReactions     { get; init; } = null!;
	public virtual DbSet<ChartTest>                ChartTests                { get; init; } = null!;
	public virtual DbSet<ChartTestGrouped>         ChartTestGroupeds         { get; init; } = null!;
	public virtual DbSet<ChartTestUnique>          ChartTestUniques          { get; init; } = null!;
	public virtual DbSet<ChartUser>                ChartUsers                { get; init; } = null!;
	public virtual DbSet<Clip>                     Clips                     { get; init; } = null!;
	public virtual DbSet<ClipNote>                 ClipNotes                 { get; init; } = null!;
	public virtual DbSet<DriveFile>                DriveFiles                { get; init; } = null!;
	public virtual DbSet<DriveFolder>              DriveFolders              { get; init; } = null!;
	public virtual DbSet<Emoji>                    Emojis                    { get; init; } = null!;
	public virtual DbSet<FollowRequest>            FollowRequests            { get; init; } = null!;
	public virtual DbSet<Following>                Followings                { get; init; } = null!;
	public virtual DbSet<GalleryLike>              GalleryLikes              { get; init; } = null!;
	public virtual DbSet<GalleryPost>              GalleryPosts              { get; init; } = null!;
	public virtual DbSet<Hashtag>                  Hashtags                  { get; init; } = null!;
	public virtual DbSet<HtmlNoteCacheEntry>       HtmlNoteCacheEntries      { get; init; } = null!;
	public virtual DbSet<HtmlUserCacheEntry>       HtmlUserCacheEntries      { get; init; } = null!;
	public virtual DbSet<Instance>                 Instances                 { get; init; } = null!;
	public virtual DbSet<MessagingMessage>         MessagingMessages         { get; init; } = null!;
	public virtual DbSet<Metum>                    Meta                      { get; init; } = null!;
	public virtual DbSet<LegacyMigrations>         Migrations                { get; init; } = null!;
	public virtual DbSet<ModerationLog>            ModerationLogs            { get; init; } = null!;
	public virtual DbSet<Muting>                   Mutings                   { get; init; } = null!;
	public virtual DbSet<Note>                     Notes                     { get; init; } = null!;
	public virtual DbSet<NoteEdit>                 NoteEdits                 { get; init; } = null!;
	public virtual DbSet<NoteFavorite>             NoteFavorites             { get; init; } = null!;
	public virtual DbSet<NoteReaction>             NoteReactions             { get; init; } = null!;
	public virtual DbSet<NoteThreadMuting>         NoteThreadMutings         { get; init; } = null!;
	public virtual DbSet<NoteUnread>               NoteUnreads               { get; init; } = null!;
	public virtual DbSet<NoteWatching>             NoteWatchings             { get; init; } = null!;
	public virtual DbSet<Notification>             Notifications             { get; init; } = null!;
	public virtual DbSet<OauthApp>                 OauthApps                 { get; init; } = null!;
	public virtual DbSet<OauthToken>               OauthTokens               { get; init; } = null!;
	public virtual DbSet<Page>                     Pages                     { get; init; } = null!;
	public virtual DbSet<PageLike>                 PageLikes                 { get; init; } = null!;
	public virtual DbSet<PasswordResetRequest>     PasswordResetRequests     { get; init; } = null!;
	public virtual DbSet<Poll>                     Polls                     { get; init; } = null!;
	public virtual DbSet<PollVote>                 PollVotes                 { get; init; } = null!;
	public virtual DbSet<PromoNote>                PromoNotes                { get; init; } = null!;
	public virtual DbSet<PromoRead>                PromoReads                { get; init; } = null!;
	public virtual DbSet<RegistrationTicket>       RegistrationTickets       { get; init; } = null!;
	public virtual DbSet<RegistryItem>             RegistryItems             { get; init; } = null!;
	public virtual DbSet<Relay>                    Relays                    { get; init; } = null!;
	public virtual DbSet<RenoteMuting>             RenoteMutings             { get; init; } = null!;
	public virtual DbSet<Session>                  Sessions                  { get; init; } = null!;
	public virtual DbSet<Signin>                   Signins                   { get; init; } = null!;
	public virtual DbSet<SwSubscription>           SwSubscriptions           { get; init; } = null!;
	public virtual DbSet<UsedUsername>             UsedUsernames             { get; init; } = null!;
	public virtual DbSet<User>                     Users                     { get; init; } = null!;
	public virtual DbSet<UserGroup>                UserGroups                { get; init; } = null!;
	public virtual DbSet<UserGroupInvitation>      UserGroupInvitations      { get; init; } = null!;
	public virtual DbSet<UserGroupInvite>          UserGroupInvites          { get; init; } = null!;
	public virtual DbSet<UserGroupJoining>         UserGroupJoinings         { get; init; } = null!;
	public virtual DbSet<UserIp>                   UserIps                   { get; init; } = null!;
	public virtual DbSet<UserKeypair>              UserKeypairs              { get; init; } = null!;
	public virtual DbSet<UserList>                 UserLists                 { get; init; } = null!;
	public virtual DbSet<UserListJoining>          UserListJoinings          { get; init; } = null!;
	public virtual DbSet<UserNotePining>           UserNotePinings           { get; init; } = null!;
	public virtual DbSet<UserPending>              UserPendings              { get; init; } = null!;
	public virtual DbSet<UserProfile>              UserProfiles              { get; init; } = null!;
	public virtual DbSet<UserPublickey>            UserPublickeys            { get; init; } = null!;
	public virtual DbSet<UserSecurityKey>          UserSecurityKeys          { get; init; } = null!;
	public virtual DbSet<Webhook>                  Webhooks                  { get; init; } = null!;

	protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
		var dataSourceBuilder = new NpgsqlDataSourceBuilder();

		var config = _config?.GetSection("Database") ??
		             throw new Exception("Configuration is missing the [Database] section");

		dataSourceBuilder.ConnectionStringBuilder.Host     = config["Host"];
		dataSourceBuilder.ConnectionStringBuilder.Username = config["Username"];
		dataSourceBuilder.ConnectionStringBuilder.Password = config["Password"];
		dataSourceBuilder.ConnectionStringBuilder.Database = config["Database"];

		dataSourceBuilder.MapEnum<Antenna.AntennaSource>();
		dataSourceBuilder.MapEnum<Note.NoteVisibility>();
		dataSourceBuilder.MapEnum<Notification.NotificationType>();
		dataSourceBuilder.MapEnum<Page.PageVisibility>();
		//dataSourceBuilder.MapEnum<Poll.PollNoteVisibility>(); // FIXME: WHY IS THIS ITS OWN ENUM
		dataSourceBuilder.MapEnum<Relay.RelayStatus>();
		dataSourceBuilder.MapEnum<UserProfile.UserProfileFFVisibility>();
		//dataSourceBuilder.MapEnum<UserProfile.MutingNotificationTypes>(); // FIXME: WHY IS THIS ITS OWN ENUM

		optionsBuilder.UseNpgsql(dataSourceBuilder.Build());
	}

	protected override void OnModelCreating(ModelBuilder modelBuilder) {
		modelBuilder
			.HasPostgresEnum<Antenna.AntennaSource>()
			//.HasPostgresEnum("log_level_enum", ["error", "warning", "info", "success", "debug"]) // TODO: not in use, add migration that removes this if it exists
			.HasPostgresEnum<Note.NoteVisibility>()
			.HasPostgresEnum<Notification.NotificationType>()
			.HasPostgresEnum<Page.PageVisibility>()
			.HasPostgresEnum("poll_notevisibility_enum", ["public", "home", "followers", "specified", "hidden"])
			.HasPostgresEnum<Relay.RelayStatus>()
			.HasPostgresEnum<UserProfile.UserProfileFFVisibility>()
			.HasPostgresEnum("user_profile_mutingnotificationtypes_enum",
			[
				"follow", "mention", "reply", "renote", "quote", "reaction", "pollVote", "pollEnded",
				"receiveFollowRequest", "followRequestAccepted", "groupInvited", "app"
			])
			.HasPostgresExtension("pg_trgm");

		modelBuilder.Entity<AbuseUserReport>(entity => {
			entity.HasKey(e => e.Id).HasName("PK_87873f5f5cc5c321a1306b2d18c");

			entity.Property(e => e.CreatedAt).HasComment("The created date of the AbuseUserReport.");
			entity.Property(e => e.Forwarded).HasDefaultValue(false);
			entity.Property(e => e.ReporterHost).HasComment("[Denormalized]");
			entity.Property(e => e.Resolved).HasDefaultValue(false);
			entity.Property(e => e.TargetUserHost).HasComment("[Denormalized]");

			entity.HasOne(d => d.Assignee).WithMany(p => p.AbuseUserReportAssignees)
			      .OnDelete(DeleteBehavior.SetNull)
			      .HasConstraintName("FK_08b883dd5fdd6f9c4c1572b36de");

			entity.HasOne(d => d.Reporter).WithMany(p => p.AbuseUserReportReporters)
			      .HasConstraintName("FK_04cc96756f89d0b7f9473e8cdf3");

			entity.HasOne(d => d.TargetUser).WithMany(p => p.AbuseUserReportTargetUsers)
			      .HasConstraintName("FK_a9021cc2e1feb5f72d3db6e9f5f");
		});

		modelBuilder.Entity<AccessToken>(entity => {
			entity.HasKey(e => e.Id).HasName("PK_f20f028607b2603deabd8182d12");

			entity.Property(e => e.CreatedAt).HasComment("The created date of the AccessToken.");
			entity.Property(e => e.Fetched).HasDefaultValue(false);
			entity.Property(e => e.Permission).HasDefaultValueSql("'{}'::character varying[]");

			entity.HasOne(d => d.App).WithMany(p => p.AccessTokens)
			      .OnDelete(DeleteBehavior.Cascade)
			      .HasConstraintName("FK_a3ff16c90cc87a82a0b5959e560");

			entity.HasOne(d => d.User).WithMany(p => p.AccessTokens)
			      .HasConstraintName("FK_9949557d0e1b2c19e5344c171e9");
		});

		modelBuilder.Entity<Announcement>(entity => {
			entity.HasKey(e => e.Id).HasName("PK_e0ef0550174fd1099a308fd18a0");

			entity.Property(e => e.CreatedAt).HasComment("The created date of the Announcement.");
			entity.Property(e => e.IsGoodNews).HasDefaultValue(false);
			entity.Property(e => e.ShowPopup).HasDefaultValue(false);
			entity.Property(e => e.UpdatedAt).HasComment("The updated date of the Announcement.");
		});

		modelBuilder.Entity<AnnouncementRead>(entity => {
			entity.HasKey(e => e.Id).HasName("PK_4b90ad1f42681d97b2683890c5e");

			entity.Property(e => e.CreatedAt).HasComment("The created date of the AnnouncementRead.");

			entity.HasOne(d => d.Announcement).WithMany(p => p.AnnouncementReads)
			      .HasConstraintName("FK_603a7b1e7aa0533c6c88e9bfafe");

			entity.HasOne(d => d.User).WithMany(p => p.AnnouncementReads)
			      .HasConstraintName("FK_8288151386172b8109f7239ab28");
		});

		modelBuilder.Entity<Antenna>(entity => {
			entity.HasKey(e => e.Id).HasName("PK_c170b99775e1dccca947c9f2d5f");

			entity.Property(e => e.CaseSensitive).HasDefaultValue(false);
			entity.Property(e => e.CreatedAt).HasComment("The created date of the Antenna.");
			entity.Property(e => e.ExcludeKeywords).HasDefaultValueSql("'[]'::jsonb");
			entity.Property(e => e.Instances).HasDefaultValueSql("'[]'::jsonb");
			entity.Property(e => e.Keywords).HasDefaultValueSql("'[]'::jsonb");
			entity.Property(e => e.Name).HasComment("The name of the Antenna.");
			entity.Property(e => e.UserId).HasComment("The owner ID.");
			entity.Property(e => e.Users).HasDefaultValueSql("'{}'::character varying[]");
			entity.Property(e => e.WithReplies).HasDefaultValue(false);

			entity.HasOne(d => d.UserGroupJoining).WithMany(p => p.Antennas)
			      .OnDelete(DeleteBehavior.Cascade)
			      .HasConstraintName("FK_ccbf5a8c0be4511133dcc50ddeb");

			entity.HasOne(d => d.User).WithMany(p => p.Antennas).HasConstraintName("FK_6446c571a0e8d0f05f01c789096");

			entity.HasOne(d => d.UserList).WithMany(p => p.Antennas)
			      .OnDelete(DeleteBehavior.Cascade)
			      .HasConstraintName("FK_709d7d32053d0dd7620f678eeb9");
		});

		modelBuilder.Entity<App>(entity => {
			entity.HasKey(e => e.Id).HasName("PK_9478629fc093d229df09e560aea");

			entity.Property(e => e.CallbackUrl).HasComment("The callbackUrl of the App.");
			entity.Property(e => e.CreatedAt).HasComment("The created date of the App.");
			entity.Property(e => e.Description).HasComment("The description of the App.");
			entity.Property(e => e.Name).HasComment("The name of the App.");
			entity.Property(e => e.Permission).HasComment("The permission of the App.");
			entity.Property(e => e.Secret).HasComment("The secret key of the App.");
			entity.Property(e => e.UserId).HasComment("The owner ID.");

			entity.HasOne(d => d.User).WithMany(p => p.Apps)
			      .OnDelete(DeleteBehavior.SetNull)
			      .HasConstraintName("FK_3f5b0899ef90527a3462d7c2cb3");
		});

		modelBuilder.Entity<AttestationChallenge>(entity => {
			entity.HasKey(e => new { e.Id, e.UserId }).HasName("PK_d0ba6786e093f1bcb497572a6b5");

			entity.Property(e => e.Challenge).HasComment("Hex-encoded sha256 hash of the challenge.");
			entity.Property(e => e.CreatedAt).HasComment("The date challenge was created for expiry purposes.");
			entity.Property(e => e.RegistrationChallenge)
			      .HasDefaultValue(false)
			      .HasComment("Indicates that the challenge is only for registration purposes if true to prevent the challenge for being used as authentication.");

			entity.HasOne(d => d.User).WithMany(p => p.AttestationChallenges)
			      .HasConstraintName("FK_f1a461a618fa1755692d0e0d592");
		});

		modelBuilder.Entity<AuthSession>(entity => {
			entity.HasKey(e => e.Id).HasName("PK_19354ed146424a728c1112a8cbf");

			entity.Property(e => e.CreatedAt).HasComment("The created date of the AuthSession.");

			entity.HasOne(d => d.App).WithMany(p => p.AuthSessions).HasConstraintName("FK_dbe037d4bddd17b03a1dc778dee");

			entity.HasOne(d => d.User).WithMany(p => p.AuthSessions)
			      .OnDelete(DeleteBehavior.Cascade)
			      .HasConstraintName("FK_c072b729d71697f959bde66ade0");
		});

		modelBuilder.Entity<Blocking>(entity => {
			entity.HasKey(e => e.Id).HasName("PK_e5d9a541cc1965ee7e048ea09dd");

			entity.Property(e => e.BlockeeId).HasComment("The blockee user ID.");
			entity.Property(e => e.BlockerId).HasComment("The blocker user ID.");
			entity.Property(e => e.CreatedAt).HasComment("The created date of the Blocking.");

			entity.HasOne(d => d.Blockee).WithMany(p => p.BlockingBlockees)
			      .HasConstraintName("FK_2cd4a2743a99671308f5417759e");

			entity.HasOne(d => d.Blocker).WithMany(p => p.BlockingBlockers)
			      .HasConstraintName("FK_0627125f1a8a42c9a1929edb552");
		});

		modelBuilder.Entity<Channel>(entity => {
			entity.HasKey(e => e.Id).HasName("PK_590f33ee6ee7d76437acf362e39");

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

			entity.HasOne(d => d.Banner).WithMany(p => p.Channels)
			      .OnDelete(DeleteBehavior.SetNull)
			      .HasConstraintName("FK_999da2bcc7efadbfe0e92d3bc19");

			entity.HasOne(d => d.User).WithMany(p => p.Channels)
			      .OnDelete(DeleteBehavior.SetNull)
			      .HasConstraintName("FK_823bae55bd81b3be6e05cff4383");
		});

		modelBuilder.Entity<ChannelFollowing>(entity => {
			entity.HasKey(e => e.Id).HasName("PK_8b104be7f7415113f2a02cd5bdd");

			entity.Property(e => e.CreatedAt).HasComment("The created date of the ChannelFollowing.");
			entity.Property(e => e.FolloweeId).HasComment("The followee channel ID.");
			entity.Property(e => e.FollowerId).HasComment("The follower user ID.");

			entity.HasOne(d => d.Followee).WithMany(p => p.ChannelFollowings)
			      .HasConstraintName("FK_0e43068c3f92cab197c3d3cd86e");

			entity.HasOne(d => d.Follower).WithMany(p => p.ChannelFollowings)
			      .HasConstraintName("FK_6d8084ec9496e7334a4602707e1");
		});

		modelBuilder.Entity<ChannelNotePining>(entity => {
			entity.HasKey(e => e.Id).HasName("PK_44f7474496bcf2e4b741681146d");

			entity.Property(e => e.CreatedAt).HasComment("The created date of the ChannelNotePining.");

			entity.HasOne(d => d.Channel).WithMany(p => p.ChannelNotePinings)
			      .HasConstraintName("FK_8125f950afd3093acb10d2db8a8");

			entity.HasOne(d => d.Note).WithMany(p => p.ChannelNotePinings)
			      .HasConstraintName("FK_10b19ef67d297ea9de325cd4502");
		});

		modelBuilder.Entity<ChartActiveUser>(entity => {
			entity.HasKey(e => e.Id).HasName("PK_317237a9f733b970604a11e314f");

			entity.Property(e => e.Read).HasDefaultValueSql("'0'::smallint");
			entity.Property(e => e.ReadWrite).HasDefaultValueSql("'0'::smallint");
			entity.Property(e => e.RegisteredOutsideMonth).HasDefaultValueSql("'0'::smallint");
			entity.Property(e => e.RegisteredOutsideWeek).HasDefaultValueSql("'0'::smallint");
			entity.Property(e => e.RegisteredOutsideYear).HasDefaultValueSql("'0'::smallint");
			entity.Property(e => e.RegisteredWithinMonth).HasDefaultValueSql("'0'::smallint");
			entity.Property(e => e.RegisteredWithinWeek).HasDefaultValueSql("'0'::smallint");
			entity.Property(e => e.RegisteredWithinYear).HasDefaultValueSql("'0'::smallint");
			entity.Property(e => e.UniqueTempRead).HasDefaultValueSql("'{}'::character varying[]");
			entity.Property(e => e.UniqueTempRegisteredOutsideMonth).HasDefaultValueSql("'{}'::character varying[]");
			entity.Property(e => e.UniqueTempRegisteredOutsideWeek).HasDefaultValueSql("'{}'::character varying[]");
			entity.Property(e => e.UniqueTempRegisteredOutsideYear).HasDefaultValueSql("'{}'::character varying[]");
			entity.Property(e => e.UniqueTempRegisteredWithinMonth).HasDefaultValueSql("'{}'::character varying[]");
			entity.Property(e => e.UniqueTempRegisteredWithinWeek).HasDefaultValueSql("'{}'::character varying[]");
			entity.Property(e => e.UniqueTempRegisteredWithinYear).HasDefaultValueSql("'{}'::character varying[]");
			entity.Property(e => e.UniqueTempWrite).HasDefaultValueSql("'{}'::character varying[]");
			entity.Property(e => e.Write).HasDefaultValueSql("'0'::smallint");
		});

		modelBuilder.Entity<ChartApRequest>(entity => {
			entity.HasKey(e => e.Id).HasName("PK_56a25cd447c7ee08876b3baf8d8");

			entity.Property(e => e.DeliverFailed).HasDefaultValue(0);
			entity.Property(e => e.DeliverSucceeded).HasDefaultValue(0);
			entity.Property(e => e.InboxReceived).HasDefaultValue(0);
		});

		modelBuilder.Entity<ChartDayActiveUser>(entity => {
			entity.HasKey(e => e.Id).HasName("PK_b1790489b14f005ae8f404f5795");

			entity.Property(e => e.Read).HasDefaultValueSql("'0'::smallint");
			entity.Property(e => e.ReadWrite).HasDefaultValueSql("'0'::smallint");
			entity.Property(e => e.RegisteredOutsideMonth).HasDefaultValueSql("'0'::smallint");
			entity.Property(e => e.RegisteredOutsideWeek).HasDefaultValueSql("'0'::smallint");
			entity.Property(e => e.RegisteredOutsideYear).HasDefaultValueSql("'0'::smallint");
			entity.Property(e => e.RegisteredWithinMonth).HasDefaultValueSql("'0'::smallint");
			entity.Property(e => e.RegisteredWithinWeek).HasDefaultValueSql("'0'::smallint");
			entity.Property(e => e.RegisteredWithinYear).HasDefaultValueSql("'0'::smallint");
			entity.Property(e => e.UniqueTempRead).HasDefaultValueSql("'{}'::character varying[]");
			entity.Property(e => e.UniqueTempRegisteredOutsideMonth).HasDefaultValueSql("'{}'::character varying[]");
			entity.Property(e => e.UniqueTempRegisteredOutsideWeek).HasDefaultValueSql("'{}'::character varying[]");
			entity.Property(e => e.UniqueTempRegisteredOutsideYear).HasDefaultValueSql("'{}'::character varying[]");
			entity.Property(e => e.UniqueTempRegisteredWithinMonth).HasDefaultValueSql("'{}'::character varying[]");
			entity.Property(e => e.UniqueTempRegisteredWithinWeek).HasDefaultValueSql("'{}'::character varying[]");
			entity.Property(e => e.UniqueTempRegisteredWithinYear).HasDefaultValueSql("'{}'::character varying[]");
			entity.Property(e => e.UniqueTempWrite).HasDefaultValueSql("'{}'::character varying[]");
			entity.Property(e => e.Write).HasDefaultValueSql("'0'::smallint");
		});

		modelBuilder.Entity<ChartDayApRequest>(entity => {
			entity.HasKey(e => e.Id).HasName("PK_9318b49daee320194e23f712e69");

			entity.Property(e => e.DeliverFailed).HasDefaultValue(0);
			entity.Property(e => e.DeliverSucceeded).HasDefaultValue(0);
			entity.Property(e => e.InboxReceived).HasDefaultValue(0);
		});

		modelBuilder.Entity<ChartDayDrive>(entity => {
			entity.HasKey(e => e.Id).HasName("PK_e7ec0de057c77c40fc8d8b62151");

			entity.Property(e => e.LocalDecCount).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.LocalDecSize).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.LocalIncCount).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.LocalIncSize).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.RemoteDecCount).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.RemoteDecSize).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.RemoteIncCount).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.RemoteIncSize).HasDefaultValueSql("'0'::bigint");
		});

		modelBuilder.Entity<ChartDayFederation>(entity => {
			entity.HasKey(e => e.Id).HasName("PK_7ca721c769f31698e0e1331e8e6");

			entity.Property(e => e.DeliveredInstances).HasDefaultValueSql("'0'::smallint");
			entity.Property(e => e.InboxInstances).HasDefaultValueSql("'0'::smallint");
			entity.Property(e => e.Pub).HasDefaultValueSql("'0'::smallint");
			entity.Property(e => e.PubActive).HasDefaultValueSql("'0'::smallint");
			entity.Property(e => e.Pubsub).HasDefaultValueSql("'0'::smallint");
			entity.Property(e => e.Stalled).HasDefaultValueSql("'0'::smallint");
			entity.Property(e => e.Sub).HasDefaultValueSql("'0'::smallint");
			entity.Property(e => e.SubActive).HasDefaultValueSql("'0'::smallint");
			entity.Property(e => e.UniqueTempDeliveredInstances).HasDefaultValueSql("'{}'::character varying[]");
			entity.Property(e => e.UniqueTempInboxInstances).HasDefaultValueSql("'{}'::character varying[]");
			entity.Property(e => e.UniqueTempStalled).HasDefaultValueSql("'{}'::character varying[]");
		});

		modelBuilder.Entity<ChartDayHashtag>(entity => {
			entity.HasKey(e => e.Id).HasName("PK_13d5a3b089344e5557f8e0980b4");

			entity.Property(e => e.LocalUsers).HasDefaultValue(0);
			entity.Property(e => e.RemoteUsers).HasDefaultValue(0);
			entity.Property(e => e.UniqueTempLocalUsers).HasDefaultValueSql("'{}'::character varying[]");
			entity.Property(e => e.UniqueTempRemoteUsers).HasDefaultValueSql("'{}'::character varying[]");
		});

		modelBuilder.Entity<ChartDayInstance>(entity => {
			entity.HasKey(e => e.Id).HasName("PK_479a8ff9d959274981087043023");

			entity.Property(e => e.DriveDecFiles).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.DriveDecUsage).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.DriveIncFiles).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.DriveIncUsage).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.DriveTotalFiles).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.FollowersDec).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.FollowersInc).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.FollowersTotal).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.FollowingDec).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.FollowingInc).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.FollowingTotal).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.NotesDec).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.NotesDiffsNormal).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.NotesDiffsRenote).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.NotesDiffsReply).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.NotesDiffsWithFile).HasDefaultValue(0);
			entity.Property(e => e.NotesInc).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.NotesTotal).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.RequestsFailed).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.RequestsReceived).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.RequestsSucceeded).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.UsersDec).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.UsersInc).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.UsersTotal).HasDefaultValueSql("'0'::bigint");
		});

		modelBuilder.Entity<ChartDayNetwork>(entity => {
			entity.HasKey(e => e.Id).HasName("PK_cac499d6f471042dfed1e7e0132");

			entity.Property(e => e.IncomingBytes).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.IncomingRequests).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.OutgoingBytes).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.OutgoingRequests).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.TotalTime).HasDefaultValueSql("'0'::bigint");
		});

		modelBuilder.Entity<ChartDayNote>(entity => {
			entity.HasKey(e => e.Id).HasName("PK_1fa4139e1f338272b758d05e090");

			entity.Property(e => e.LocalDec).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.LocalDiffsNormal).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.LocalDiffsRenote).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.LocalDiffsReply).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.LocalDiffsWithFile).HasDefaultValue(0);
			entity.Property(e => e.LocalInc).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.LocalTotal).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.RemoteDec).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.RemoteDiffsNormal).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.RemoteDiffsRenote).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.RemoteDiffsReply).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.RemoteDiffsWithFile).HasDefaultValue(0);
			entity.Property(e => e.RemoteInc).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.RemoteTotal).HasDefaultValueSql("'0'::bigint");
		});

		modelBuilder.Entity<ChartDayPerUserDrive>(entity => {
			entity.HasKey(e => e.Id).HasName("PK_1ae135254c137011645da7f4045");

			entity.Property(e => e.DecCount).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.DecSize).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.IncCount).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.IncSize).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.TotalCount).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.TotalSize).HasDefaultValueSql("'0'::bigint");
		});

		modelBuilder.Entity<ChartDayPerUserFollowing>(entity => {
			entity.HasKey(e => e.Id).HasName("PK_68ce6b67da57166da66fc8fb27e");

			entity.Property(e => e.LocalFollowersDec).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.LocalFollowersInc).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.LocalFollowersTotal).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.LocalFollowingsDec).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.LocalFollowingsInc).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.LocalFollowingsTotal).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.RemoteFollowersDec).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.RemoteFollowersInc).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.RemoteFollowersTotal).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.RemoteFollowingsDec).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.RemoteFollowingsInc).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.RemoteFollowingsTotal).HasDefaultValueSql("'0'::bigint");
		});

		modelBuilder.Entity<ChartDayPerUserNote>(entity => {
			entity.HasKey(e => e.Id).HasName("PK_58bab6b6d3ad9310cbc7460fd28");

			entity.Property(e => e.Dec).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.DiffsNormal).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.DiffsRenote).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.DiffsReply).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.DiffsWithFile).HasDefaultValueSql("'0'::smallint");
			entity.Property(e => e.Inc).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.Total).HasDefaultValueSql("'0'::bigint");
		});

		modelBuilder.Entity<ChartDayPerUserReaction>(entity => {
			entity.HasKey(e => e.Id).HasName("PK_8af24e2d51ff781a354fe595eda");

			entity.Property(e => e.LocalCount).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.RemoteCount).HasDefaultValueSql("'0'::bigint");
		});

		modelBuilder.Entity<ChartDayUser>(entity => {
			entity.HasKey(e => e.Id).HasName("PK_d7f7185abb9851f70c4726c54bd");

			entity.Property(e => e.LocalDec).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.LocalInc).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.LocalTotal).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.RemoteDec).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.RemoteInc).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.RemoteTotal).HasDefaultValueSql("'0'::bigint");
		});

		modelBuilder.Entity<ChartDrive>(entity => {
			entity.HasKey(e => e.Id).HasName("PK_f96bc548a765cd4b3b354221ce7");

			entity.Property(e => e.LocalDecCount).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.LocalDecSize).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.LocalIncCount).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.LocalIncSize).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.RemoteDecCount).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.RemoteDecSize).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.RemoteIncCount).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.RemoteIncSize).HasDefaultValueSql("'0'::bigint");
		});

		modelBuilder.Entity<ChartFederation>(entity => {
			entity.HasKey(e => e.Id).HasName("PK_b39dcd31a0fe1a7757e348e85fd");

			entity.Property(e => e.DeliveredInstances).HasDefaultValueSql("'0'::smallint");
			entity.Property(e => e.InboxInstances).HasDefaultValueSql("'0'::smallint");
			entity.Property(e => e.Pub).HasDefaultValueSql("'0'::smallint");
			entity.Property(e => e.PubActive).HasDefaultValueSql("'0'::smallint");
			entity.Property(e => e.Pubsub).HasDefaultValueSql("'0'::smallint");
			entity.Property(e => e.Stalled).HasDefaultValueSql("'0'::smallint");
			entity.Property(e => e.Sub).HasDefaultValueSql("'0'::smallint");
			entity.Property(e => e.SubActive).HasDefaultValueSql("'0'::smallint");
			entity.Property(e => e.UniqueTempDeliveredInstances).HasDefaultValueSql("'{}'::character varying[]");
			entity.Property(e => e.UniqueTempInboxInstances).HasDefaultValueSql("'{}'::character varying[]");
			entity.Property(e => e.UniqueTempStalled).HasDefaultValueSql("'{}'::character varying[]");
		});

		modelBuilder.Entity<ChartHashtag>(entity => {
			entity.HasKey(e => e.Id).HasName("PK_c32f1ea2b44a5d2f7881e37f8f9");

			entity.Property(e => e.LocalUsers).HasDefaultValue(0);
			entity.Property(e => e.RemoteUsers).HasDefaultValue(0);
			entity.Property(e => e.UniqueTempLocalUsers).HasDefaultValueSql("'{}'::character varying[]");
			entity.Property(e => e.UniqueTempRemoteUsers).HasDefaultValueSql("'{}'::character varying[]");
		});

		modelBuilder.Entity<ChartInstance>(entity => {
			entity.HasKey(e => e.Id).HasName("PK_1267c67c7c2d47b4903975f2c00");

			entity.Property(e => e.DriveDecFiles).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.DriveDecUsage).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.DriveIncFiles).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.DriveIncUsage).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.DriveTotalFiles).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.FollowersDec).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.FollowersInc).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.FollowersTotal).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.FollowingDec).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.FollowingInc).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.FollowingTotal).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.NotesDec).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.NotesDiffsNormal).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.NotesDiffsRenote).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.NotesDiffsReply).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.NotesDiffsWithFile).HasDefaultValue(0);
			entity.Property(e => e.NotesInc).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.NotesTotal).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.RequestsFailed).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.RequestsReceived).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.RequestsSucceeded).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.UsersDec).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.UsersInc).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.UsersTotal).HasDefaultValueSql("'0'::bigint");
		});

		modelBuilder.Entity<ChartNetwork>(entity => {
			entity.HasKey(e => e.Id).HasName("PK_bc4290c2e27fad14ef0c1ca93f3");

			entity.Property(e => e.IncomingBytes).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.IncomingRequests).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.OutgoingBytes).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.OutgoingRequests).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.TotalTime).HasDefaultValueSql("'0'::bigint");
		});

		modelBuilder.Entity<ChartNote>(entity => {
			entity.HasKey(e => e.Id).HasName("PK_0aec823fa85c7f901bdb3863b14");

			entity.Property(e => e.LocalDec).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.LocalDiffsNormal).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.LocalDiffsRenote).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.LocalDiffsReply).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.LocalDiffsWithFile).HasDefaultValue(0);
			entity.Property(e => e.LocalInc).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.LocalTotal).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.RemoteDec).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.RemoteDiffsNormal).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.RemoteDiffsRenote).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.RemoteDiffsReply).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.RemoteDiffsWithFile).HasDefaultValue(0);
			entity.Property(e => e.RemoteInc).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.RemoteTotal).HasDefaultValueSql("'0'::bigint");
		});

		modelBuilder.Entity<ChartPerUserDrive>(entity => {
			entity.HasKey(e => e.Id).HasName("PK_d0ef23d24d666e1a44a0cd3d208");

			entity.Property(e => e.DecCount).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.DecSize).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.IncCount).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.IncSize).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.TotalCount).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.TotalSize).HasDefaultValueSql("'0'::bigint");
		});

		modelBuilder.Entity<ChartPerUserFollowing>(entity => {
			entity.HasKey(e => e.Id).HasName("PK_85bb1b540363a29c2fec83bd907");

			entity.Property(e => e.LocalFollowersDec).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.LocalFollowersInc).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.LocalFollowersTotal).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.LocalFollowingsDec).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.LocalFollowingsInc).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.LocalFollowingsTotal).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.RemoteFollowersDec).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.RemoteFollowersInc).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.RemoteFollowersTotal).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.RemoteFollowingsDec).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.RemoteFollowingsInc).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.RemoteFollowingsTotal).HasDefaultValueSql("'0'::bigint");
		});

		modelBuilder.Entity<ChartPerUserNote>(entity => {
			entity.HasKey(e => e.Id).HasName("PK_334acf6e915af2f29edc11b8e50");

			entity.Property(e => e.Dec).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.DiffsNormal).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.DiffsRenote).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.DiffsReply).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.DiffsWithFile).HasDefaultValueSql("'0'::smallint");
			entity.Property(e => e.Inc).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.Total).HasDefaultValueSql("'0'::bigint");
		});

		modelBuilder.Entity<ChartPerUserReaction>(entity => {
			entity.HasKey(e => e.Id).HasName("PK_984f54dae441e65b633e8d27a7f");

			entity.Property(e => e.LocalCount).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.RemoteCount).HasDefaultValueSql("'0'::bigint");
		});

		modelBuilder.Entity<ChartTest>(entity => {
			entity.HasKey(e => e.Id).HasName("PK_b4bc31dffbd1b785276a3ecfc1e");

			entity.HasIndex(e => e.Date, "IDX_dab383a36f3c9db4a0c9b02cf3")
			      .IsUnique()
			      .HasFilter("(\"group\" IS NULL)");
		});

		modelBuilder.Entity<ChartTestGrouped>(entity => {
			entity.HasKey(e => e.Id).HasName("PK_f4a2b175d308695af30d4293272");

			entity.HasIndex(e => e.Date, "IDX_da522b4008a9f5d7743b87ad55")
			      .IsUnique()
			      .HasFilter("(\"group\" IS NULL)");
		});

		modelBuilder.Entity<ChartTestUnique>(entity => {
			entity.HasKey(e => e.Id).HasName("PK_409bac9c97cc612d8500012319d");

			entity.HasIndex(e => e.Date, "IDX_16effb2e888f6763673b579f80")
			      .IsUnique()
			      .HasFilter("(\"group\" IS NULL)");

			entity.Property(e => e.Foo).HasDefaultValueSql("'{}'::character varying[]");
		});

		modelBuilder.Entity<ChartUser>(entity => {
			entity.HasKey(e => e.Id).HasName("PK_4dfcf2c78d03524b9eb2c99d328");

			entity.Property(e => e.LocalDec).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.LocalInc).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.LocalTotal).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.RemoteDec).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.RemoteInc).HasDefaultValueSql("'0'::bigint");
			entity.Property(e => e.RemoteTotal).HasDefaultValueSql("'0'::bigint");
		});

		modelBuilder.Entity<Clip>(entity => {
			entity.HasKey(e => e.Id).HasName("PK_f0685dac8d4dd056d7255670b75");

			entity.Property(e => e.CreatedAt).HasComment("The created date of the Clip.");
			entity.Property(e => e.Description).HasComment("The description of the Clip.");
			entity.Property(e => e.IsPublic).HasDefaultValue(false);
			entity.Property(e => e.Name).HasComment("The name of the Clip.");
			entity.Property(e => e.UserId).HasComment("The owner ID.");

			entity.HasOne(d => d.User).WithMany(p => p.Clips).HasConstraintName("FK_2b5ec6c574d6802c94c80313fb2");
		});

		modelBuilder.Entity<ClipNote>(entity => {
			entity.HasKey(e => e.Id).HasName("PK_e94cda2f40a99b57e032a1a738b");

			entity.Property(e => e.ClipId).HasComment("The clip ID.");
			entity.Property(e => e.NoteId).HasComment("The note ID.");

			entity.HasOne(d => d.Clip).WithMany(p => p.ClipNotes).HasConstraintName("FK_ebe99317bbbe9968a0c6f579adf");

			entity.HasOne(d => d.Note).WithMany(p => p.ClipNotes).HasConstraintName("FK_a012eaf5c87c65da1deb5fdbfa3");
		});

		modelBuilder.Entity<DriveFile>(entity => {
			entity.HasKey(e => e.Id).HasName("PK_43ddaaaf18c9e68029b7cbb032e");

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
			entity.Property(e => e.Md5).HasComment("The MD5 hash of the DriveFile.");
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

			entity.HasOne(d => d.Folder).WithMany(p => p.DriveFiles)
			      .OnDelete(DeleteBehavior.SetNull)
			      .HasConstraintName("FK_bb90d1956dafc4068c28aa7560a");

			entity.HasOne(d => d.User).WithMany(p => p.DriveFiles)
			      .OnDelete(DeleteBehavior.SetNull)
			      .HasConstraintName("FK_860fa6f6c7df5bb887249fba22e");
		});

		modelBuilder.Entity<DriveFolder>(entity => {
			entity.HasKey(e => e.Id).HasName("PK_7a0c089191f5ebdc214e0af808a");

			entity.Property(e => e.CreatedAt).HasComment("The created date of the DriveFolder.");
			entity.Property(e => e.Name).HasComment("The name of the DriveFolder.");
			entity.Property(e => e.ParentId)
			      .HasComment("The parent folder ID. If null, it means the DriveFolder is located in root.");
			entity.Property(e => e.UserId).HasComment("The owner ID.");

			entity.HasOne(d => d.Parent).WithMany(p => p.InverseParent)
			      .OnDelete(DeleteBehavior.SetNull)
			      .HasConstraintName("FK_00ceffb0cdc238b3233294f08f2");

			entity.HasOne(d => d.User).WithMany(p => p.DriveFolders)
			      .OnDelete(DeleteBehavior.Cascade)
			      .HasConstraintName("FK_f4fc06e49c0171c85f1c48060d2");
		});

		modelBuilder.Entity<Emoji>(entity => {
			entity.HasKey(e => e.Id).HasName("PK_df74ce05e24999ee01ea0bc50a3");

			entity.Property(e => e.Aliases).HasDefaultValueSql("'{}'::character varying[]");
			entity.Property(e => e.Height).HasComment("Image height");
			entity.Property(e => e.PublicUrl).HasDefaultValueSql("''::character varying");
			entity.Property(e => e.Width).HasComment("Image width");
		});

		modelBuilder.Entity<FollowRequest>(entity => {
			entity.HasKey(e => e.Id).HasName("PK_53a9aa3725f7a3deb150b39dbfc");

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

			entity.HasOne(d => d.Followee).WithMany(p => p.FollowRequestFollowees)
			      .HasConstraintName("FK_12c01c0d1a79f77d9f6c15fadd2");

			entity.HasOne(d => d.Follower).WithMany(p => p.FollowRequestFollowers)
			      .HasConstraintName("FK_a7fd92dd6dc519e6fb435dd108f");
		});

		modelBuilder.Entity<Following>(entity => {
			entity.HasKey(e => e.Id).HasName("PK_c76c6e044bdf76ecf8bfb82a645");

			entity.Property(e => e.CreatedAt).HasComment("The created date of the Following.");
			entity.Property(e => e.FolloweeHost).HasComment("[Denormalized]");
			entity.Property(e => e.FolloweeId).HasComment("The followee user ID.");
			entity.Property(e => e.FolloweeInbox).HasComment("[Denormalized]");
			entity.Property(e => e.FolloweeSharedInbox).HasComment("[Denormalized]");
			entity.Property(e => e.FollowerHost).HasComment("[Denormalized]");
			entity.Property(e => e.FollowerId).HasComment("The follower user ID.");
			entity.Property(e => e.FollowerInbox).HasComment("[Denormalized]");
			entity.Property(e => e.FollowerSharedInbox).HasComment("[Denormalized]");

			entity.HasOne(d => d.Followee).WithMany(p => p.FollowingFollowees)
			      .HasConstraintName("FK_24e0042143a18157b234df186c3");

			entity.HasOne(d => d.Follower).WithMany(p => p.FollowingFollowers)
			      .HasConstraintName("FK_6516c5a6f3c015b4eed39978be5");
		});

		modelBuilder.Entity<GalleryLike>(entity => {
			entity.HasKey(e => e.Id).HasName("PK_853ab02be39b8de45cd720cc15f");

			entity.HasOne(d => d.Post).WithMany(p => p.GalleryLikes)
			      .HasConstraintName("FK_b1cb568bfe569e47b7051699fc8");

			entity.HasOne(d => d.User).WithMany(p => p.GalleryLikes)
			      .HasConstraintName("FK_8fd5215095473061855ceb948cf");
		});

		modelBuilder.Entity<GalleryPost>(entity => {
			entity.HasKey(e => e.Id).HasName("PK_8e90d7b6015f2c4518881b14753");

			entity.Property(e => e.CreatedAt).HasComment("The created date of the GalleryPost.");
			entity.Property(e => e.FileIds).HasDefaultValueSql("'{}'::character varying[]");
			entity.Property(e => e.IsSensitive)
			      .HasDefaultValue(false)
			      .HasComment("Whether the post is sensitive.");
			entity.Property(e => e.LikedCount).HasDefaultValue(0);
			entity.Property(e => e.Tags).HasDefaultValueSql("'{}'::character varying[]");
			entity.Property(e => e.UpdatedAt).HasComment("The updated date of the GalleryPost.");
			entity.Property(e => e.UserId).HasComment("The ID of author.");

			entity.HasOne(d => d.User).WithMany(p => p.GalleryPosts)
			      .HasConstraintName("FK_985b836dddd8615e432d7043ddb");
		});

		modelBuilder.Entity<Hashtag>(entity => {
			entity.HasKey(e => e.Id).HasName("PK_cb36eb8af8412bfa978f1165d78");

			entity.Property(e => e.AttachedLocalUsersCount).HasDefaultValue(0);
			entity.Property(e => e.AttachedRemoteUsersCount).HasDefaultValue(0);
			entity.Property(e => e.AttachedUsersCount).HasDefaultValue(0);
			entity.Property(e => e.MentionedLocalUsersCount).HasDefaultValue(0);
			entity.Property(e => e.MentionedRemoteUsersCount).HasDefaultValue(0);
			entity.Property(e => e.MentionedUsersCount).HasDefaultValue(0);
		});

		modelBuilder.Entity<HtmlNoteCacheEntry>(entity => {
			entity.HasKey(e => e.NoteId).HasName("PK_6ef86ec901b2017cbe82d3a8286");

			entity.HasOne(d => d.Note).WithOne(p => p.HtmlNoteCacheEntry)
			      .HasConstraintName("FK_6ef86ec901b2017cbe82d3a8286");
		});

		modelBuilder.Entity<HtmlUserCacheEntry>(entity => {
			entity.HasKey(e => e.UserId).HasName("PK_920b9474e3c9cae3f3c37c057e1");

			entity.Property(e => e.Fields).HasDefaultValueSql("'[]'::jsonb");

			entity.HasOne(d => d.User).WithOne(p => p.HtmlUserCacheEntry)
			      .HasConstraintName("FK_920b9474e3c9cae3f3c37c057e1");
		});

		modelBuilder.Entity<Instance>(entity => {
			entity.HasKey(e => e.Id).HasName("PK_eaf60e4a0c399c9935413e06474");

			entity.Property(e => e.CaughtAt).HasComment("The caught date of the Instance.");
			entity.Property(e => e.FollowersCount).HasDefaultValue(0);
			entity.Property(e => e.FollowingCount).HasDefaultValue(0);
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

		modelBuilder.Entity<MessagingMessage>(entity => {
			entity.HasKey(e => e.Id).HasName("PK_db398fd79dc95d0eb8c30456eaa");

			entity.Property(e => e.CreatedAt).HasComment("The created date of the MessagingMessage.");
			entity.Property(e => e.GroupId).HasComment("The recipient group ID.");
			entity.Property(e => e.IsRead).HasDefaultValue(false);
			entity.Property(e => e.Reads).HasDefaultValueSql("'{}'::character varying[]");
			entity.Property(e => e.RecipientId).HasComment("The recipient user ID.");
			entity.Property(e => e.UserId).HasComment("The sender user ID.");

			entity.HasOne(d => d.File).WithMany(p => p.MessagingMessages)
			      .OnDelete(DeleteBehavior.Cascade)
			      .HasConstraintName("FK_535def119223ac05ad3fa9ef64b");

			entity.HasOne(d => d.Group).WithMany(p => p.MessagingMessages)
			      .OnDelete(DeleteBehavior.Cascade)
			      .HasConstraintName("FK_2c4be03b446884f9e9c502135be");

			entity.HasOne(d => d.Recipient).WithMany(p => p.MessagingMessageRecipients)
			      .OnDelete(DeleteBehavior.Cascade)
			      .HasConstraintName("FK_cac14a4e3944454a5ce7daa5142");

			entity.HasOne(d => d.User).WithMany(p => p.MessagingMessageUsers)
			      .HasConstraintName("FK_5377c307783fce2b6d352e1203b");
		});

		modelBuilder.Entity<Metum>(entity => {
			entity.HasKey(e => e.Id).HasName("PK_c4c17a6c2bd7651338b60fc590b");

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
			entity.Property(e => e.ObjectStorageS3forcePathStyle).HasDefaultValue(true);
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

		modelBuilder.Entity<LegacyMigrations>(entity => {
			entity.HasKey(e => e.Id).HasName("PK_8c82d7f526340ab734260ea46be");
		});

		modelBuilder.Entity<ModerationLog>(entity => {
			entity.HasKey(e => e.Id).HasName("PK_d0adca6ecfd068db83e4526cc26");

			entity.Property(e => e.CreatedAt).HasComment("The created date of the ModerationLog.");

			entity.HasOne(d => d.User).WithMany(p => p.ModerationLogs)
			      .HasConstraintName("FK_a08ad074601d204e0f69da9a954");
		});

		modelBuilder.Entity<Muting>(entity => {
			entity.HasKey(e => e.Id).HasName("PK_2e92d06c8b5c602eeb27ca9ba48");

			entity.Property(e => e.CreatedAt).HasComment("The created date of the Muting.");
			entity.Property(e => e.MuteeId).HasComment("The mutee user ID.");
			entity.Property(e => e.MuterId).HasComment("The muter user ID.");

			entity.HasOne(d => d.Mutee).WithMany(p => p.MutingMutees)
			      .HasConstraintName("FK_ec96b4fed9dae517e0dbbe0675c");

			entity.HasOne(d => d.Muter).WithMany(p => p.MutingMuters)
			      .HasConstraintName("FK_93060675b4a79a577f31d260c67");
		});

		modelBuilder.Entity<Note>(entity => {
			entity.HasKey(e => e.Id).HasName("PK_96d0c172a4fba276b1bbed43058");

			entity.HasIndex(e => e.Mentions, "IDX_NOTE_MENTIONS").HasMethod("gin");

			entity.HasIndex(e => e.Tags, "IDX_NOTE_TAGS").HasMethod("gin");

			entity.HasIndex(e => e.VisibleUserIds, "IDX_NOTE_VISIBLE_USER_IDS").HasMethod("gin");

			entity.HasIndex(e => e.Text, "note_text_fts_idx")
			      .HasMethod("gin")
			      .HasOperators("gin_trgm_ops");

			entity.Property(e => e.AttachedFileTypes).HasDefaultValueSql("'{}'::character varying[]");
			entity.Property(e => e.ChannelId).HasComment("The ID of source channel.");
			entity.Property(e => e.CreatedAt).HasComment("The created date of the Note.");
			entity.Property(e => e.Emojis).HasDefaultValueSql("'{}'::character varying[]");
			entity.Property(e => e.FileIds).HasDefaultValueSql("'{}'::character varying[]");
			entity.Property(e => e.HasPoll).HasDefaultValue(false);
			entity.Property(e => e.LocalOnly).HasDefaultValue(false);
			entity.Property(e => e.MentionedRemoteUsers).HasDefaultValueSql("'[]'::text");
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

			entity.HasOne(d => d.Channel).WithMany(p => p.Notes)
			      .OnDelete(DeleteBehavior.Cascade)
			      .HasConstraintName("FK_f22169eb10657bded6d875ac8f9");

			entity.HasOne(d => d.Renote).WithMany(p => p.InverseRenote)
			      .OnDelete(DeleteBehavior.Cascade)
			      .HasConstraintName("FK_52ccc804d7c69037d558bac4c96");

			entity.HasOne(d => d.Reply).WithMany(p => p.InverseReply)
			      .OnDelete(DeleteBehavior.Cascade)
			      .HasConstraintName("FK_17cb3553c700a4985dff5a30ff5");

			entity.HasOne(d => d.User).WithMany(p => p.Notes).HasConstraintName("FK_5b87d9d19127bd5d92026017a7b");
		});

		modelBuilder.Entity<NoteEdit>(entity => {
			entity.HasKey(e => e.Id).HasName("PK_736fc6e0d4e222ecc6f82058e08");

			entity.Property(e => e.FileIds).HasDefaultValueSql("'{}'::character varying[]");
			entity.Property(e => e.NoteId).HasComment("The ID of note.");
			entity.Property(e => e.UpdatedAt).HasComment("The updated date of the Note.");

			entity.HasOne(d => d.Note).WithMany(p => p.NoteEdits).HasConstraintName("FK_702ad5ae993a672e4fbffbcd38c");
		});

		modelBuilder.Entity<NoteFavorite>(entity => {
			entity.HasKey(e => e.Id).HasName("PK_af0da35a60b9fa4463a62082b36");

			entity.Property(e => e.CreatedAt).HasComment("The created date of the NoteFavorite.");

			entity.HasOne(d => d.Note).WithMany(p => p.NoteFavorites)
			      .HasConstraintName("FK_0e00498f180193423c992bc4370");

			entity.HasOne(d => d.User).WithMany(p => p.NoteFavorites)
			      .HasConstraintName("FK_47f4b1892f5d6ba8efb3057d81a");
		});

		modelBuilder.Entity<NoteReaction>(entity => {
			entity.HasKey(e => e.Id).HasName("PK_767ec729b108799b587a3fcc9cf");

			entity.Property(e => e.CreatedAt).HasComment("The created date of the NoteReaction.");

			entity.HasOne(d => d.Note).WithMany(p => p.NoteReactions)
			      .HasConstraintName("FK_45145e4953780f3cd5656f0ea6a");

			entity.HasOne(d => d.User).WithMany(p => p.NoteReactions)
			      .HasConstraintName("FK_13761f64257f40c5636d0ff95ee");
		});

		modelBuilder.Entity<NoteThreadMuting>(entity => {
			entity.HasKey(e => e.Id).HasName("PK_ec5936d94d1a0369646d12a3a47");

			entity.HasOne(d => d.User).WithMany(p => p.NoteThreadMutings)
			      .HasConstraintName("FK_29c11c7deb06615076f8c95b80a");
		});

		modelBuilder.Entity<NoteUnread>(entity => {
			entity.HasKey(e => e.Id).HasName("PK_1904eda61a784f57e6e51fa9c1f");

			entity.Property(e => e.NoteChannelId).HasComment("[Denormalized]");
			entity.Property(e => e.NoteUserId).HasComment("[Denormalized]");

			entity.HasOne(d => d.Note).WithMany(p => p.NoteUnreads).HasConstraintName("FK_e637cba4dc4410218c4251260e4");

			entity.HasOne(d => d.User).WithMany(p => p.NoteUnreads).HasConstraintName("FK_56b0166d34ddae49d8ef7610bb9");
		});

		modelBuilder.Entity<NoteWatching>(entity => {
			entity.HasKey(e => e.Id).HasName("PK_49286fdb23725945a74aa27d757");

			entity.Property(e => e.CreatedAt).HasComment("The created date of the NoteWatching.");
			entity.Property(e => e.NoteId).HasComment("The target Note ID.");
			entity.Property(e => e.NoteUserId).HasComment("[Denormalized]");
			entity.Property(e => e.UserId).HasComment("The watcher ID.");

			entity.HasOne(d => d.Note).WithMany(p => p.NoteWatchings)
			      .HasConstraintName("FK_03e7028ab8388a3f5e3ce2a8619");

			entity.HasOne(d => d.User).WithMany(p => p.NoteWatchings)
			      .HasConstraintName("FK_b0134ec406e8d09a540f8182888");
		});

		modelBuilder.Entity<Notification>(entity => {
			entity.HasKey(e => e.Id).HasName("PK_705b6c7cdf9b2c2ff7ac7872cb7");

			entity.Property(e => e.CreatedAt).HasComment("The created date of the Notification.");
			entity.Property(e => e.IsRead)
			      .HasDefaultValue(false)
			      .HasComment("Whether the notification was read.");
			entity.Property(e => e.NotifieeId).HasComment("The ID of recipient user of the Notification.");
			entity.Property(e => e.NotifierId).HasComment("The ID of sender user of the Notification.");

			entity.HasOne(d => d.AppAccessToken).WithMany(p => p.Notifications)
			      .OnDelete(DeleteBehavior.Cascade)
			      .HasConstraintName("FK_e22bf6bda77b6adc1fd9e75c8c9");

			entity.HasOne(d => d.FollowRequest).WithMany(p => p.Notifications)
			      .OnDelete(DeleteBehavior.Cascade)
			      .HasConstraintName("FK_bd7fab507621e635b32cd31892c");

			entity.HasOne(d => d.Note).WithMany(p => p.Notifications)
			      .OnDelete(DeleteBehavior.Cascade)
			      .HasConstraintName("FK_769cb6b73a1efe22ddf733ac453");

			entity.HasOne(d => d.Notifiee).WithMany(p => p.NotificationNotifiees)
			      .HasConstraintName("FK_3c601b70a1066d2c8b517094cb9");

			entity.HasOne(d => d.Notifier).WithMany(p => p.NotificationNotifiers)
			      .OnDelete(DeleteBehavior.Cascade)
			      .HasConstraintName("FK_3b4e96eec8d36a8bbb9d02aa710");

			entity.HasOne(d => d.UserGroupInvitation).WithMany(p => p.Notifications)
			      .OnDelete(DeleteBehavior.Cascade)
			      .HasConstraintName("FK_8fe87814e978053a53b1beb7e98");
		});

		modelBuilder.Entity<OauthApp>(entity => {
			entity.HasKey(e => e.Id).HasName("PK_3256b97c0a3ee2d67240805dca4");

			entity.Property(e => e.ClientId).HasComment("The client id of the OAuth application");
			entity.Property(e => e.ClientSecret).HasComment("The client secret of the OAuth application");
			entity.Property(e => e.CreatedAt).HasComment("The created date of the OAuth application");
			entity.Property(e => e.Name).HasComment("The name of the OAuth application");
			entity.Property(e => e.RedirectUris).HasComment("The redirect URIs of the OAuth application");
			entity.Property(e => e.Scopes).HasComment("The scopes requested by the OAuth application");
			entity.Property(e => e.Website).HasComment("The website of the OAuth application");
		});

		modelBuilder.Entity<OauthToken>(entity => {
			entity.HasKey(e => e.Id).HasName("PK_7e6a25a3cc4395d1658f5b89c73");

			entity.Property(e => e.Active).HasComment("Whether or not the token has been activated");
			entity.Property(e => e.Code).HasComment("The auth code for the OAuth token");
			entity.Property(e => e.CreatedAt).HasComment("The created date of the OAuth token");
			entity.Property(e => e.RedirectUri).HasComment("The redirect URI of the OAuth token");
			entity.Property(e => e.Scopes).HasComment("The scopes requested by the OAuth token");
			entity.Property(e => e.Token).HasComment("The OAuth token");

			entity.HasOne(d => d.App).WithMany(p => p.OauthTokens).HasConstraintName("FK_6d3ef28ea647b1449ba79690874");

			entity.HasOne(d => d.User).WithMany(p => p.OauthTokens).HasConstraintName("FK_f6b4b1ac66b753feab5d831ba04");
		});

		modelBuilder.Entity<Page>(entity => {
			entity.HasKey(e => e.Id).HasName("PK_742f4117e065c5b6ad21b37ba1f");

			entity.Property(e => e.Content).HasDefaultValueSql("'[]'::jsonb");
			entity.Property(e => e.CreatedAt).HasComment("The created date of the Page.");
			entity.Property(e => e.HideTitleWhenPinned).HasDefaultValue(false);
			entity.Property(e => e.LikedCount).HasDefaultValue(0);
			entity.Property(e => e.Script).HasDefaultValueSql("''::character varying");
			entity.Property(e => e.UpdatedAt).HasComment("The updated date of the Page.");
			entity.Property(e => e.UserId).HasComment("The ID of author.");
			entity.Property(e => e.Variables).HasDefaultValueSql("'[]'::jsonb");
			entity.Property(e => e.VisibleUserIds).HasDefaultValueSql("'{}'::character varying[]");

			entity.HasOne(d => d.EyeCatchingImage).WithMany(p => p.Pages)
			      .OnDelete(DeleteBehavior.Cascade)
			      .HasConstraintName("FK_a9ca79ad939bf06066b81c9d3aa");

			entity.HasOne(d => d.User).WithMany(p => p.Pages).HasConstraintName("FK_ae1d917992dd0c9d9bbdad06c4a");
		});

		modelBuilder.Entity<PageLike>(entity => {
			entity.HasKey(e => e.Id).HasName("PK_813f034843af992d3ae0f43c64c");

			entity.HasOne(d => d.Page).WithMany(p => p.PageLikes).HasConstraintName("FK_cf8782626dced3176038176a847");

			entity.HasOne(d => d.User).WithMany(p => p.PageLikes).HasConstraintName("FK_0e61efab7f88dbb79c9166dbb48");
		});

		modelBuilder.Entity<PasswordResetRequest>(entity => {
			entity.HasKey(e => e.Id).HasName("PK_fcf4b02eae1403a2edaf87fd074");

			entity.HasOne(d => d.User).WithMany(p => p.PasswordResetRequests)
			      .HasConstraintName("FK_4bb7fd4a34492ae0e6cc8d30ac8");
		});

		modelBuilder.Entity<Poll>(entity => {
			entity.HasKey(e => e.NoteId).HasName("PK_da851e06d0dfe2ef397d8b1bf1b");

			entity.Property(e => e.Choices).HasDefaultValueSql("'{}'::character varying[]");
			entity.Property(e => e.UserHost).HasComment("[Denormalized]");
			entity.Property(e => e.UserId).HasComment("[Denormalized]");

			entity.HasOne(d => d.Note).WithOne(p => p.Poll).HasConstraintName("FK_da851e06d0dfe2ef397d8b1bf1b");
		});

		modelBuilder.Entity<PollVote>(entity => {
			entity.HasKey(e => e.Id).HasName("PK_fd002d371201c472490ba89c6a0");

			entity.Property(e => e.CreatedAt).HasComment("The created date of the PollVote.");

			entity.HasOne(d => d.Note).WithMany(p => p.PollVotes).HasConstraintName("FK_aecfbd5ef60374918e63ee95fa7");

			entity.HasOne(d => d.User).WithMany(p => p.PollVotes).HasConstraintName("FK_66d2bd2ee31d14bcc23069a89f8");
		});

		modelBuilder.Entity<PromoNote>(entity => {
			entity.HasKey(e => e.NoteId).HasName("PK_e263909ca4fe5d57f8d4230dd5c");

			entity.Property(e => e.UserId).HasComment("[Denormalized]");

			entity.HasOne(d => d.Note).WithOne(p => p.PromoNote).HasConstraintName("FK_e263909ca4fe5d57f8d4230dd5c");
		});

		modelBuilder.Entity<PromoRead>(entity => {
			entity.HasKey(e => e.Id).HasName("PK_61917c1541002422b703318b7c9");

			entity.Property(e => e.CreatedAt).HasComment("The created date of the PromoRead.");

			entity.HasOne(d => d.Note).WithMany(p => p.PromoReads).HasConstraintName("FK_a46a1a603ecee695d7db26da5f4");

			entity.HasOne(d => d.User).WithMany(p => p.PromoReads).HasConstraintName("FK_9657d55550c3d37bfafaf7d4b05");
		});

		modelBuilder.Entity<RegistrationTicket>(entity => {
			entity.HasKey(e => e.Id).HasName("PK_f11696b6fafcf3662d4292734f8");
		});

		modelBuilder.Entity<RegistryItem>(entity => {
			entity.HasKey(e => e.Id).HasName("PK_64b3f7e6008b4d89b826cd3af95");

			entity.Property(e => e.CreatedAt).HasComment("The created date of the RegistryItem.");
			entity.Property(e => e.Key).HasComment("The key of the RegistryItem.");
			entity.Property(e => e.Scope).HasDefaultValueSql("'{}'::character varying[]");
			entity.Property(e => e.UpdatedAt).HasComment("The updated date of the RegistryItem.");
			entity.Property(e => e.UserId).HasComment("The owner ID.");
			entity.Property(e => e.Value)
			      .HasDefaultValueSql("'{}'::jsonb")
			      .HasComment("The value of the RegistryItem.");

			entity.HasOne(d => d.User).WithMany(p => p.RegistryItems)
			      .HasConstraintName("FK_fb9d21ba0abb83223263df6bcb3");
		});

		modelBuilder.Entity<Relay>(entity => { entity.HasKey(e => e.Id).HasName("PK_78ebc9cfddf4292633b7ba57aee"); });

		modelBuilder.Entity<RenoteMuting>(entity => {
			entity.HasKey(e => e.Id).HasName("PK_renoteMuting_id");

			entity.Property(e => e.CreatedAt).HasComment("The created date of the Muting.");
			entity.Property(e => e.MuteeId).HasComment("The mutee user ID.");
			entity.Property(e => e.MuterId).HasComment("The muter user ID.");

			entity.HasOne(d => d.Mutee).WithMany(p => p.RenoteMutingMutees)
			      .HasConstraintName("FK_7eac97594bcac5ffcf2068089b6");

			entity.HasOne(d => d.Muter).WithMany(p => p.RenoteMutingMuters)
			      .HasConstraintName("FK_7aa72a5fe76019bfe8e5e0e8b7d");
		});

		modelBuilder.Entity<Session>(entity => {
			entity.HasKey(e => e.Id).HasName("PK_f55da76ac1c3ac420f444d2ff11");

			entity.Property(e => e.Active)
			      .HasComment("Whether or not the token has been activated (i.e. 2fa has been confirmed)");
			entity.Property(e => e.CreatedAt).HasComment("The created date of the OAuth token");
			entity.Property(e => e.Token).HasComment("The authorization token");

			entity.HasOne(d => d.User).WithMany(p => p.Sessions).HasConstraintName("FK_3d2f174ef04fb312fdebd0ddc53");
		});

		modelBuilder.Entity<Signin>(entity => {
			entity.HasKey(e => e.Id).HasName("PK_9e96ddc025712616fc492b3b588");

			entity.Property(e => e.CreatedAt).HasComment("The created date of the Signin.");

			entity.HasOne(d => d.User).WithMany(p => p.Signins).HasConstraintName("FK_2c308dbdc50d94dc625670055f7");
		});

		modelBuilder.Entity<SwSubscription>(entity => {
			entity.HasKey(e => e.Id).HasName("PK_e8f763631530051b95eb6279b91");

			entity.Property(e => e.SendReadMessage).HasDefaultValue(false);

			entity.HasOne(d => d.User).WithMany(p => p.SwSubscriptions)
			      .HasConstraintName("FK_97754ca6f2baff9b4abb7f853dd");
		});

		modelBuilder.Entity<UsedUsername>(entity => {
			entity.HasKey(e => e.Username).HasName("PK_78fd79d2d24c6ac2f4cc9a31a5d");
		});

		modelBuilder.Entity<User>(entity => {
			entity.HasKey(e => e.Id).HasName("PK_cace4a159ff9f2512dd42373760");

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
			entity.Property(e => e.Name).HasComment("The name of the User.");
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

			entity.HasOne(d => d.Avatar).WithOne(p => p.UserAvatar)
			      .OnDelete(DeleteBehavior.SetNull)
			      .HasConstraintName("FK_58f5c71eaab331645112cf8cfa5");

			entity.HasOne(d => d.Banner).WithOne(p => p.UserBanner)
			      .OnDelete(DeleteBehavior.SetNull)
			      .HasConstraintName("FK_afc64b53f8db3707ceb34eb28e2");
		});

		modelBuilder.Entity<UserGroup>(entity => {
			entity.HasKey(e => e.Id).HasName("PK_3c29fba6fe013ec8724378ce7c9");

			entity.Property(e => e.CreatedAt).HasComment("The created date of the UserGroup.");
			entity.Property(e => e.IsPrivate).HasDefaultValue(false);
			entity.Property(e => e.UserId).HasComment("The ID of owner.");

			entity.HasOne(d => d.User).WithMany(p => p.UserGroups).HasConstraintName("FK_3d6b372788ab01be58853003c93");
		});

		modelBuilder.Entity<UserGroupInvitation>(entity => {
			entity.HasKey(e => e.Id).HasName("PK_160c63ec02bf23f6a5c5e8140d6");

			entity.Property(e => e.CreatedAt).HasComment("The created date of the UserGroupInvitation.");
			entity.Property(e => e.UserGroupId).HasComment("The group ID.");
			entity.Property(e => e.UserId).HasComment("The user ID.");

			entity.HasOne(d => d.UserGroup).WithMany(p => p.UserGroupInvitations)
			      .HasConstraintName("FK_5cc8c468090e129857e9fecce5a");

			entity.HasOne(d => d.User).WithMany(p => p.UserGroupInvitations)
			      .HasConstraintName("FK_bfbc6305547539369fe73eb144a");
		});

		modelBuilder.Entity<UserGroupInvite>(entity => {
			entity.HasKey(e => e.Id).HasName("PK_3893884af0d3a5f4d01e7921a97");

			entity.HasOne(d => d.UserGroup).WithMany(p => p.UserGroupInvites)
			      .HasConstraintName("FK_e10924607d058004304611a436a");

			entity.HasOne(d => d.User).WithMany(p => p.UserGroupInvites)
			      .HasConstraintName("FK_1039988afa3bf991185b277fe03");
		});

		modelBuilder.Entity<UserGroupJoining>(entity => {
			entity.HasKey(e => e.Id).HasName("PK_15f2425885253c5507e1599cfe7");

			entity.Property(e => e.CreatedAt).HasComment("The created date of the UserGroupJoining.");
			entity.Property(e => e.UserGroupId).HasComment("The group ID.");
			entity.Property(e => e.UserId).HasComment("The user ID.");

			entity.HasOne(d => d.UserGroup).WithMany(p => p.UserGroupJoinings)
			      .HasConstraintName("FK_67dc758bc0566985d1b3d399865");

			entity.HasOne(d => d.User).WithMany(p => p.UserGroupJoinings)
			      .HasConstraintName("FK_f3a1b4bd0c7cabba958a0c0b231");
		});

		modelBuilder.Entity<UserIp>(entity => { entity.HasKey(e => e.Id).HasName("PK_2c44ddfbf7c0464d028dcef325e"); });

		modelBuilder.Entity<UserKeypair>(entity => {
			entity.HasKey(e => e.UserId).HasName("PK_f4853eb41ab722fe05f81cedeb6");

			entity.HasOne(d => d.User).WithOne(p => p.UserKeypair).HasConstraintName("FK_f4853eb41ab722fe05f81cedeb6");
		});

		modelBuilder.Entity<UserList>(entity => {
			entity.HasKey(e => e.Id).HasName("PK_87bab75775fd9b1ff822b656402");

			entity.Property(e => e.CreatedAt).HasComment("The created date of the UserList.");
			entity.Property(e => e.HideFromHomeTl)
			      .HasDefaultValue(false)
			      .HasComment("Whether posts from list members should be hidden from the home timeline.");
			entity.Property(e => e.Name).HasComment("The name of the UserList.");
			entity.Property(e => e.UserId).HasComment("The owner ID.");

			entity.HasOne(d => d.User).WithMany(p => p.UserLists).HasConstraintName("FK_b7fcefbdd1c18dce86687531f99");
		});

		modelBuilder.Entity<UserListJoining>(entity => {
			entity.HasKey(e => e.Id).HasName("PK_11abb3768da1c5f8de101c9df45");

			entity.Property(e => e.CreatedAt).HasComment("The created date of the UserListJoining.");
			entity.Property(e => e.UserId).HasComment("The user ID.");
			entity.Property(e => e.UserListId).HasComment("The list ID.");

			entity.HasOne(d => d.User).WithMany(p => p.UserListJoinings)
			      .HasConstraintName("FK_d844bfc6f3f523a05189076efaa");

			entity.HasOne(d => d.UserList).WithMany(p => p.UserListJoinings)
			      .HasConstraintName("FK_605472305f26818cc93d1baaa74");
		});

		modelBuilder.Entity<UserNotePining>(entity => {
			entity.HasKey(e => e.Id).HasName("PK_a6a2dad4ae000abce2ea9d9b103");

			entity.Property(e => e.CreatedAt).HasComment("The created date of the UserNotePinings.");

			entity.HasOne(d => d.Note).WithMany(p => p.UserNotePinings)
			      .HasConstraintName("FK_68881008f7c3588ad7ecae471cf");

			entity.HasOne(d => d.User).WithMany(p => p.UserNotePinings)
			      .HasConstraintName("FK_bfbc6f79ba4007b4ce5097f08d6");
		});

		modelBuilder.Entity<UserPending>(entity => {
			entity.HasKey(e => e.Id).HasName("PK_d4c84e013c98ec02d19b8fbbafa");
		});

		modelBuilder.Entity<UserProfile>(entity => {
			entity.HasKey(e => e.UserId).HasName("PK_51cb79b5555effaf7d69ba1cff9");

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

			entity.HasOne(d => d.PinnedPage).WithOne(p => p.UserProfile)
			      .OnDelete(DeleteBehavior.SetNull)
			      .HasConstraintName("FK_6dc44f1ceb65b1e72bacef2ca27");

			entity.HasOne(d => d.User).WithOne(p => p.UserProfile).HasConstraintName("FK_51cb79b5555effaf7d69ba1cff9");
		});

		modelBuilder.Entity<UserPublickey>(entity => {
			entity.HasKey(e => e.UserId).HasName("PK_10c146e4b39b443ede016f6736d");

			entity.HasOne(d => d.User).WithOne(p => p.UserPublickey)
			      .HasConstraintName("FK_10c146e4b39b443ede016f6736d");
		});

		modelBuilder.Entity<UserSecurityKey>(entity => {
			entity.HasKey(e => e.Id).HasName("PK_3e508571121ab39c5f85d10c166");

			entity.Property(e => e.Id).HasComment("Variable-length id given to navigator.credentials.get()");
			entity.Property(e => e.LastUsed)
			      .HasComment("The date of the last time the UserSecurityKey was successfully validated.");
			entity.Property(e => e.Name).HasComment("User-defined name for this key");
			entity.Property(e => e.PublicKey)
			      .HasComment("Variable-length public key used to verify attestations (hex-encoded).");

			entity.HasOne(d => d.User).WithMany(p => p.UserSecurityKeys)
			      .HasConstraintName("FK_ff9ca3b5f3ee3d0681367a9b447");
		});

		modelBuilder.Entity<Webhook>(entity => {
			entity.HasKey(e => e.Id).HasName("PK_e6765510c2d078db49632b59020");

			entity.Property(e => e.Active).HasDefaultValue(true);
			entity.Property(e => e.CreatedAt).HasComment("The created date of the Antenna.");
			entity.Property(e => e.Name).HasComment("The name of the Antenna.");
			entity.Property(e => e.On).HasDefaultValueSql("'{}'::character varying[]");
			entity.Property(e => e.UserId).HasComment("The owner ID.");

			entity.HasOne(d => d.User).WithMany(p => p.Webhooks).HasConstraintName("FK_f272c8c8805969e6a6449c77b3c");
		});
	}
}