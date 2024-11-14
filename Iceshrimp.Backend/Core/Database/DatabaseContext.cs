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
	public virtual DbSet<NoteThread>           NoteThreads           { get; init; } = null!;
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
	public virtual DbSet<CacheEntry>           CacheStore            { get; init; } = null!;
	public virtual DbSet<Job>                  Jobs                  { get; init; } = null!;
	public virtual DbSet<Filter>               Filters               { get; init; } = null!;
	public virtual DbSet<PluginStoreEntry>     PluginStore           { get; init; } = null!;
	public virtual DbSet<PolicyConfiguration>  PolicyConfiguration   { get; init; } = null!;
	public virtual DbSet<DataProtectionKey>    DataProtectionKeys    { get; init; } = null!;

	public static NpgsqlDataSource GetDataSource(Config.DatabaseSection config)
	{
		var dataSourceBuilder = new NpgsqlDataSourceBuilder
		{
			ConnectionStringBuilder =
			{
				Host         = config.Host,
				Port         = config.Port,
				Username     = config.Username,
				Password     = config.Password,
				Database     = config.Database,
				MaxPoolSize  = config.MaxConnections,
				Multiplexing = config.Multiplexing,
				Options      = "-c jit=off"
			}
		};

		return ConfigureDataSource(dataSourceBuilder, config);
	}

	private static NpgsqlDataSource ConfigureDataSource(
		NpgsqlDataSourceBuilder dataSourceBuilder, Config.DatabaseSection config
	)
	{
		dataSourceBuilder.MapEnum<Antenna.AntennaSource>();
		dataSourceBuilder.MapEnum<Note.NoteVisibility>();
		dataSourceBuilder.MapEnum<Notification.NotificationType>();
		dataSourceBuilder.MapEnum<Page.PageVisibility>();
		dataSourceBuilder.MapEnum<Relay.RelayStatus>();
		dataSourceBuilder.MapEnum<UserProfile.UserProfileFFVisibility>();
		dataSourceBuilder.MapEnum<Marker.MarkerType>();
		dataSourceBuilder.MapEnum<PushSubscription.PushPolicy>();
		dataSourceBuilder.MapEnum<Job.JobStatus>();
		dataSourceBuilder.MapEnum<Filter.FilterContext>();
		dataSourceBuilder.MapEnum<Filter.FilterAction>();

		dataSourceBuilder.EnableDynamicJson();

		if (config.ParameterLogging)
			dataSourceBuilder.EnableParameterLogging();

		return dataSourceBuilder.Build();
	}

	public static void Configure(
		DbContextOptionsBuilder optionsBuilder, NpgsqlDataSource dataSource, Config.DatabaseSection config
	)
	{
		optionsBuilder.UseNpgsql(dataSource, options =>
		{
			options.MapEnum<Antenna.AntennaSource>("antenna_src_enum");
			options.MapEnum<Note.NoteVisibility>("note_visibility_enum");
			options.MapEnum<Notification.NotificationType>("notification_type_enum");
			options.MapEnum<Page.PageVisibility>("page_visibility_enum");
			options.MapEnum<Relay.RelayStatus>("relay_status_enum");
			options.MapEnum<UserProfile.UserProfileFFVisibility>("user_profile_ffvisibility_enum");
			options.MapEnum<Marker.MarkerType>("marker_type_enum");
			options.MapEnum<PushSubscription.PushPolicy>("push_subscription_policy_enum");
			options.MapEnum<Job.JobStatus>("job_status");
			options.MapEnum<Filter.FilterContext>("filter_context_enum");
			options.MapEnum<Filter.FilterAction>("filter_action_enum");
		});

		optionsBuilder.UseProjectables(options => { options.CompatibilityMode(CompatibilityMode.Full); });
		optionsBuilder.UseExceptionProcessor();

		if (config.ParameterLogging)
			optionsBuilder.EnableSensitiveDataLogging();
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
			.HasPostgresEnum<Job.JobStatus>()
			.HasPostgresEnum<Filter.FilterContext>()
			.HasPostgresEnum<Filter.FilterAction>()
			.HasPostgresExtension("pg_trgm");

		modelBuilder
			.HasDbFunction(typeof(DatabaseContext).GetMethod(nameof(NoteAncestors), [typeof(string), typeof(int)])!)
			.HasName("note_ancestors");
		modelBuilder
			.HasDbFunction(typeof(DatabaseContext).GetMethod(nameof(Conversations), [typeof(string)])!)
			.HasName("conversations");
		modelBuilder
			.HasDbFunction(typeof(Note).GetMethod(nameof(Note.InternalRawAttachments), [typeof(string)])!)
			.HasName("note_attachments_raw");

		modelBuilder.Entity<DataProtectionKey>().ToTable("data_protection_keys");
		modelBuilder.ApplyConfigurationsFromAssembly(typeof(DatabaseContext).Assembly);
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

	public IQueryable<Note> NoteDescendants(string noteId, int depth, int limit)
		=> Notes.FromSql($"""
		                  SELECT * FROM note WHERE id IN (
		                    WITH RECURSIVE search_tree(id, path) AS (
		                      SELECT id, ARRAY[id]::VARCHAR[]
		                      FROM note
		                      WHERE id = {noteId}
		                      UNION ALL (
		                        SELECT note.id, path || note.id
		                        FROM search_tree
		                        JOIN note ON note."replyId" = search_tree.id
		                        WHERE COALESCE(array_length(path, 1) < {depth + 1}, TRUE) AND NOT note.id = ANY(path)
		                      )
		                    )
		                    SELECT id
		                    FROM search_tree
		                    WHERE id <> {noteId}
		                    LIMIT {limit}
		                  )
		                  """);

	public IQueryable<Note> NoteDescendants(Note note, int depth, int breadth)
		=> NoteDescendants(note.Id, depth, breadth);

	public IQueryable<Note> Conversations(string userId)
		=> FromExpression(() => Conversations(userId));

	public IQueryable<Note> Conversations(User user)
		=> FromExpression(() => Conversations(user.Id));

	public IQueryable<Job> GetJob(string queue)
		=> Database.SqlQuery<Job>($"""
		                           UPDATE "jobs" SET "status" = 'running', "started_at" = now()
		                           WHERE "id" = (
		                               SELECT "id" FROM "jobs"
		                               WHERE queue = {queue} AND status = 'queued'
		                               ORDER BY COALESCE("delayed_until", "queued_at")
		                               LIMIT 1
		                               FOR UPDATE SKIP LOCKED)
		                           RETURNING "jobs".*;
		                           """);

	public Task<int> GetJobRunningCount(string queue, CancellationToken token) =>
		Jobs.CountAsync(p => p.Queue == queue && p.Status == Job.JobStatus.Running, token);

	public Task<int> GetJobQueuedCount(string queue, CancellationToken token) =>
		Jobs.CountAsync(p => p.Queue == queue && p.Status == Job.JobStatus.Queued, token);

	public async Task<bool> IsDatabaseEmpty()
		=> !await Database.SqlQuery<object>($"""
		                                     select s.nspname from pg_class c
		                                     join pg_namespace s on s.oid = c.relnamespace
		                                     where s.nspname in ('public')
		                                     """)
		                  .AnyAsync();
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

		var config = configuration.GetSection("Database").Get<Config.DatabaseSection>() ??
		             throw new Exception("Failed to initialize database: Failed to load configuration");

		// Required to make `dotnet ef database update` work correctly 
		config.Multiplexing = false;

		var dataSource = DatabaseContext.GetDataSource(config);
		var builder    = new DbContextOptionsBuilder<DatabaseContext>();
		DatabaseContext.Configure(builder, dataSource, config);
		return new DatabaseContext(builder.Options);
	}
}