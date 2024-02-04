using System;
using System.Collections.Generic;
using Iceshrimp.Backend.Core.Database.Tables;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Iceshrimp.Backend.Core.Database.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:antenna_src_enum", "home,all,users,list,group,instances")
                .Annotation("Npgsql:Enum:log_level_enum", "error,warning,info,success,debug")
                .Annotation("Npgsql:Enum:note_visibility_enum", "public,home,followers,specified,hidden")
                .Annotation("Npgsql:Enum:notification_type_enum", "follow,mention,reply,renote,quote,reaction,pollVote,pollEnded,receiveFollowRequest,followRequestAccepted,groupInvited,app")
                .Annotation("Npgsql:Enum:page_visibility_enum", "public,followers,specified")
                .Annotation("Npgsql:Enum:poll_notevisibility_enum", "public,home,followers,specified,hidden")
                .Annotation("Npgsql:Enum:relay_status_enum", "requesting,accepted,rejected")
                .Annotation("Npgsql:Enum:user_profile_ffvisibility_enum", "public,followers,private")
                .Annotation("Npgsql:Enum:user_profile_mutingnotificationtypes_enum", "follow,mention,reply,renote,quote,reaction,pollVote,pollEnded,receiveFollowRequest,followRequestAccepted,groupInvited,app")
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,");

            migrationBuilder.CreateTable(
                name: "announcement",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    createdAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "The created date of the Announcement."),
                    text = table.Column<string>(type: "character varying(8192)", maxLength: 8192, nullable: false),
                    title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    imageUrl = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    updatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "The updated date of the Announcement."),
                    showPopup = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    isGoodNews = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_e0ef0550174fd1099a308fd18a0", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "emoji",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    updatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    host = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    originalUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    uri = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    aliases = table.Column<List<string>>(type: "character varying(128)[]", nullable: false, defaultValueSql: "'{}'::character varying[]"),
                    category = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    publicUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false, defaultValueSql: "''::character varying"),
                    license = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    width = table.Column<int>(type: "integer", nullable: true, comment: "Image width"),
                    height = table.Column<int>(type: "integer", nullable: true, comment: "Image height")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_df74ce05e24999ee01ea0bc50a3", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "hashtag",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    mentionedUserIds = table.Column<List<string>>(type: "character varying(32)[]", nullable: false),
                    mentionedUsersCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    mentionedLocalUserIds = table.Column<List<string>>(type: "character varying(32)[]", nullable: false),
                    mentionedLocalUsersCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    mentionedRemoteUserIds = table.Column<List<string>>(type: "character varying(32)[]", nullable: false),
                    mentionedRemoteUsersCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    attachedUserIds = table.Column<List<string>>(type: "character varying(32)[]", nullable: false),
                    attachedUsersCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    attachedLocalUserIds = table.Column<List<string>>(type: "character varying(32)[]", nullable: false),
                    attachedLocalUsersCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    attachedRemoteUserIds = table.Column<List<string>>(type: "character varying(32)[]", nullable: false),
                    attachedRemoteUsersCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cb36eb8af8412bfa978f1165d78", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "instance",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    caughtAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "The caught date of the Instance."),
                    host = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false, comment: "The host of the Instance."),
                    usersCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0, comment: "The count of the users of the Instance."),
                    notesCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0, comment: "The count of the notes of the Instance."),
                    followingCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    followersCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    latestRequestSentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    latestStatus = table.Column<int>(type: "integer", nullable: true),
                    latestRequestReceivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    lastCommunicatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    isNotResponding = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    softwareName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true, comment: "The software of the Instance."),
                    softwareVersion = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    openRegistrations = table.Column<bool>(type: "boolean", nullable: true),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    description = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: true),
                    maintainerName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    maintainerEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    infoUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    isSuspended = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    iconUrl = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: true),
                    themeColor = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    faviconUrl = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_eaf60e4a0c399c9935413e06474", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "meta",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    description = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    maintainerName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    maintainerEmail = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    disableRegistration = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    disableLocalTimeline = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    disableGlobalTimeline = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    langs = table.Column<List<string>>(type: "character varying(64)[]", nullable: false, defaultValueSql: "'{}'::character varying[]"),
                    hiddenTags = table.Column<List<string>>(type: "character varying(256)[]", nullable: false, defaultValueSql: "'{}'::character varying[]"),
                    blockedHosts = table.Column<List<string>>(type: "character varying(256)[]", nullable: false, defaultValueSql: "'{}'::character varying[]"),
                    mascotImageUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true, defaultValueSql: "'/static-assets/badges/info.png'::character varying"),
                    bannerUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    errorImageUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true, defaultValueSql: "'/static-assets/badges/error.png'::character varying"),
                    iconUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    cacheRemoteFiles = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    enableRecaptcha = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    recaptchaSiteKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    recaptchaSecretKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    localDriveCapacityMb = table.Column<int>(type: "integer", nullable: false, defaultValue: 1024, comment: "Drive capacity of a local user (MB)"),
                    remoteDriveCapacityMb = table.Column<int>(type: "integer", nullable: false, defaultValue: 32, comment: "Drive capacity of a remote user (MB)"),
                    summalyProxy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    enableEmail = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    email = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    smtpSecure = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    smtpHost = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    smtpPort = table.Column<int>(type: "integer", nullable: true),
                    smtpUser = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    smtpPass = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    swPublicKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    swPrivateKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    enableGithubIntegration = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    githubClientId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    githubClientSecret = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    enableDiscordIntegration = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    discordClientId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    discordClientSecret = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    pinnedUsers = table.Column<List<string>>(type: "character varying(256)[]", nullable: false, defaultValueSql: "'{}'::character varying[]"),
                    ToSUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    repositoryUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false, defaultValueSql: "'https://iceshrimp.dev/iceshrimp/iceshrimp'::character varying"),
                    feedbackUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true, defaultValueSql: "'https://iceshrimp.dev/iceshrimp/iceshrimp/issues/new'::character varying"),
                    useObjectStorage = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    objectStorageBucket = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    objectStoragePrefix = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    objectStorageBaseUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    objectStorageEndpoint = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    objectStorageRegion = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    objectStorageAccessKey = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    objectStorageSecretKey = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    objectStoragePort = table.Column<int>(type: "integer", nullable: true),
                    objectStorageUseSSL = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    objectStorageUseProxy = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    enableHcaptcha = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    hcaptchaSiteKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    hcaptchaSecretKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    objectStorageSetPublicRead = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    pinnedPages = table.Column<List<string>>(type: "character varying(512)[]", nullable: false, defaultValueSql: "'{/featured,/channels,/explore,/pages,/about-iceshrimp}'::character varying[]"),
                    backgroundImageUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    logoImageUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    pinnedClipId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    objectStorageS3ForcePathStyle = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    allowedHosts = table.Column<List<string>>(type: "character varying(256)[]", nullable: false, defaultValueSql: "'{}'::character varying[]"),
                    secureMode = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    privateMode = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deeplAuthKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    deeplIsPro = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    emailRequiredForSignup = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    themeColor = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    defaultLightTheme = table.Column<string>(type: "character varying(8192)", maxLength: 8192, nullable: true),
                    defaultDarkTheme = table.Column<string>(type: "character varying(8192)", maxLength: 8192, nullable: true),
                    enableIpLogging = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    enableActiveEmailValidation = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    customMOTD = table.Column<List<string>>(type: "character varying(256)[]", nullable: false, defaultValueSql: "'{}'::character varying[]"),
                    customSplashIcons = table.Column<List<string>>(type: "character varying(256)[]", nullable: false, defaultValueSql: "'{}'::character varying[]"),
                    disableRecommendedTimeline = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    recommendedInstances = table.Column<List<string>>(type: "character varying(256)[]", nullable: false, defaultValueSql: "'{}'::character varying[]"),
                    defaultReaction = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false, defaultValueSql: "'⭐'::character varying"),
                    libreTranslateApiUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    libreTranslateApiKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    silencedHosts = table.Column<List<string>>(type: "character varying(256)[]", nullable: false, defaultValueSql: "'{}'::character varying[]"),
                    experimentalFeatures = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
                    enableServerMachineStats = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    enableIdenticonGeneration = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    donationLink = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    autofollowedAccount = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_c4c17a6c2bd7651338b60fc590b", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "oauth_app",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    createdAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "The created date of the OAuth application"),
                    clientId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, comment: "The client id of the OAuth application"),
                    clientSecret = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, comment: "The client secret of the OAuth application"),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false, comment: "The name of the OAuth application"),
                    website = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true, comment: "The website of the OAuth application"),
                    scopes = table.Column<List<string>>(type: "character varying(64)[]", nullable: false, comment: "The scopes requested by the OAuth application"),
                    redirectUris = table.Column<List<string>>(type: "character varying(512)[]", nullable: false, comment: "The redirect URIs of the OAuth application")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_3256b97c0a3ee2d67240805dca4", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "registration_ticket",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    createdAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_f11696b6fafcf3662d4292734f8", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "relay",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    inbox = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    status = table.Column<Relay.RelayStatus>(type: "relay_status_enum", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_78ebc9cfddf4292633b7ba57aee", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "used_username",
                columns: table => new
                {
                    username = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    createdAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_78fd79d2d24c6ac2f4cc9a31a5d", x => x.username);
                });

            migrationBuilder.CreateTable(
                name: "user_pending",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    createdAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    code = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    username = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    email = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    password = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_d4c84e013c98ec02d19b8fbbafa", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "abuse_user_report",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    createdAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "The created date of the AbuseUserReport."),
                    targetUserId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    reporterId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    assigneeId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    resolved = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    comment = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    targetUserHost = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true, comment: "[Denormalized]"),
                    reporterHost = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true, comment: "[Denormalized]"),
                    forwarded = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_87873f5f5cc5c321a1306b2d18c", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "access_token",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    createdAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "The created date of the AccessToken."),
                    token = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    userId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    appId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    lastUsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    session = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    iconUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    permission = table.Column<List<string>>(type: "character varying(64)[]", nullable: false, defaultValueSql: "'{}'::character varying[]"),
                    fetched = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_f20f028607b2603deabd8182d12", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "announcement_read",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    userId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    announcementId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    createdAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "The created date of the AnnouncementRead.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_4b90ad1f42681d97b2683890c5e", x => x.id);
                    table.ForeignKey(
                        name: "FK_603a7b1e7aa0533c6c88e9bfafe",
                        column: x => x.announcementId,
                        principalTable: "announcement",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "antenna",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    createdAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "The created date of the Antenna."),
                    userId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, comment: "The owner ID."),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false, comment: "The name of the Antenna."),
                    src = table.Column<Antenna.AntennaSource>(type: "antenna_src_enum", nullable: false),
                    userListId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    keywords = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'[]'::jsonb"),
                    withFile = table.Column<bool>(type: "boolean", nullable: false),
                    expression = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    notify = table.Column<bool>(type: "boolean", nullable: false),
                    caseSensitive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    withReplies = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    userGroupJoiningId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    users = table.Column<List<string>>(type: "character varying(1024)[]", nullable: false, defaultValueSql: "'{}'::character varying[]"),
                    excludeKeywords = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'[]'::jsonb"),
                    instances = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'[]'::jsonb")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_c170b99775e1dccca947c9f2d5f", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "app",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    createdAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "The created date of the App."),
                    userId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true, comment: "The owner ID."),
                    secret = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, comment: "The secret key of the App."),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false, comment: "The name of the App."),
                    description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false, comment: "The description of the App."),
                    permission = table.Column<List<string>>(type: "character varying(64)[]", nullable: false, comment: "The permission of the App."),
                    callbackUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true, comment: "The callbackUrl of the App.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_9478629fc093d229df09e560aea", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "attestation_challenge",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    userId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    challenge = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, comment: "Hex-encoded sha256 hash of the challenge."),
                    createdAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "The date challenge was created for expiry purposes."),
                    registrationChallenge = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Indicates that the challenge is only for registration purposes if true to prevent the challenge for being used as authentication.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_d0ba6786e093f1bcb497572a6b5", x => new { x.id, x.userId });
                });

            migrationBuilder.CreateTable(
                name: "auth_session",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    createdAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "The created date of the AuthSession."),
                    token = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    userId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    appId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_19354ed146424a728c1112a8cbf", x => x.id);
                    table.ForeignKey(
                        name: "FK_dbe037d4bddd17b03a1dc778dee",
                        column: x => x.appId,
                        principalTable: "app",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "blocking",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    createdAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "The created date of the Blocking."),
                    blockeeId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, comment: "The blockee user ID."),
                    blockerId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, comment: "The blocker user ID.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_e5d9a541cc1965ee7e048ea09dd", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "channel",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    createdAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "The created date of the Channel."),
                    lastNotedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    userId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true, comment: "The owner ID."),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false, comment: "The name of the Channel."),
                    description = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true, comment: "The description of the Channel."),
                    bannerId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true, comment: "The ID of banner Channel."),
                    notesCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0, comment: "The count of notes."),
                    usersCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0, comment: "The count of users.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_590f33ee6ee7d76437acf362e39", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "channel_following",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    createdAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "The created date of the ChannelFollowing."),
                    followeeId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, comment: "The followee channel ID."),
                    followerId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, comment: "The follower user ID.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_8b104be7f7415113f2a02cd5bdd", x => x.id);
                    table.ForeignKey(
                        name: "FK_0e43068c3f92cab197c3d3cd86e",
                        column: x => x.followeeId,
                        principalTable: "channel",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "channel_note_pining",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    createdAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "The created date of the ChannelNotePining."),
                    channelId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    noteId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_44f7474496bcf2e4b741681146d", x => x.id);
                    table.ForeignKey(
                        name: "FK_8125f950afd3093acb10d2db8a8",
                        column: x => x.channelId,
                        principalTable: "channel",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "clip",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    createdAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "The created date of the Clip."),
                    userId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, comment: "The owner ID."),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false, comment: "The name of the Clip."),
                    isPublic = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    description = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true, comment: "The description of the Clip.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_f0685dac8d4dd056d7255670b75", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "clip_note",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    noteId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, comment: "The note ID."),
                    clipId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, comment: "The clip ID.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_e94cda2f40a99b57e032a1a738b", x => x.id);
                    table.ForeignKey(
                        name: "FK_ebe99317bbbe9968a0c6f579adf",
                        column: x => x.clipId,
                        principalTable: "clip",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "drive_file",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    createdAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "The created date of the DriveFile."),
                    userId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true, comment: "The owner ID."),
                    userHost = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true, comment: "The host of owner. It will be null if the user in local."),
                    md5 = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, comment: "The MD5 hash of the DriveFile."),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false, comment: "The file name of the DriveFile."),
                    type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false, comment: "The content type (MIME) of the DriveFile."),
                    size = table.Column<int>(type: "integer", nullable: false, comment: "The file size (bytes) of the DriveFile."),
                    comment = table.Column<string>(type: "character varying(8192)", maxLength: 8192, nullable: true, comment: "The comment of the DriveFile."),
                    properties = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb", comment: "The any properties of the DriveFile. For example, it includes image width/height."),
                    storedInternal = table.Column<bool>(type: "boolean", nullable: false),
                    url = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false, comment: "The URL of the DriveFile."),
                    thumbnailUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true, comment: "The URL of the thumbnail of the DriveFile."),
                    webpublicUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true, comment: "The URL of the webpublic of the DriveFile."),
                    accessKey = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    thumbnailAccessKey = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    webpublicAccessKey = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    uri = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true, comment: "The URI of the DriveFile. it will be null when the DriveFile is local."),
                    src = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    folderId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true, comment: "The parent folder ID. If null, it means the DriveFile is located in root."),
                    isSensitive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Whether the DriveFile is NSFW."),
                    isLink = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Whether the DriveFile is direct link to remote server."),
                    blurhash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true, comment: "The BlurHash string."),
                    webpublicType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    requestHeaders = table.Column<string>(type: "jsonb", nullable: true, defaultValueSql: "'{}'::jsonb"),
                    requestIp = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_43ddaaaf18c9e68029b7cbb032e", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    createdAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "The created date of the User."),
                    updatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "The updated date of the User."),
                    lastFetchedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    username = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false, comment: "The username of the User."),
                    usernameLower = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false, comment: "The username (lowercased) of the User."),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true, comment: "The name of the User."),
                    followersCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0, comment: "The count of followers."),
                    followingCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0, comment: "The count of following."),
                    notesCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0, comment: "The count of notes."),
                    avatarId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true, comment: "The ID of avatar DriveFile."),
                    bannerId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true, comment: "The ID of banner DriveFile."),
                    tags = table.Column<List<string>>(type: "character varying(128)[]", nullable: false, defaultValueSql: "'{}'::character varying[]"),
                    isSuspended = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Whether the User is suspended."),
                    isSilenced = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Whether the User is silenced."),
                    isLocked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Whether the User is locked."),
                    isBot = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Whether the User is a bot."),
                    isCat = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Whether the User is a cat."),
                    isAdmin = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Whether the User is the admin."),
                    isModerator = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Whether the User is a moderator."),
                    emojis = table.Column<List<string>>(type: "character varying(128)[]", nullable: false, defaultValueSql: "'{}'::character varying[]"),
                    host = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true, comment: "The host of the User. It will be null if the origin of the user is local."),
                    inbox = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true, comment: "The inbox URL of the User. It will be null if the origin of the user is local."),
                    sharedInbox = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true, comment: "The sharedInbox URL of the User. It will be null if the origin of the user is local."),
                    featured = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true, comment: "The featured URL of the User. It will be null if the origin of the user is local."),
                    uri = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true, comment: "The URI of the User. It will be null if the origin of the user is local."),
                    token = table.Column<string>(type: "character(16)", fixedLength: true, maxLength: 16, nullable: true, comment: "The native access token of the User. It will be null if the origin of the user is local."),
                    isExplorable = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true, comment: "Whether the User is explorable."),
                    followersUri = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true, comment: "The URI of the user Follower Collection. It will be null if the origin of the user is local."),
                    lastActiveDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    hideOnlineStatus = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    isDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Whether the User is deleted."),
                    driveCapacityOverrideMb = table.Column<int>(type: "integer", nullable: true, comment: "Overrides user drive capacity limit"),
                    movedToUri = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true, comment: "The URI of the new account of the User"),
                    alsoKnownAs = table.Column<string>(type: "text", nullable: true, comment: "URIs the user is known as too"),
                    speakAsCat = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true, comment: "Whether to speak as a cat if isCat."),
                    avatarUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true, comment: "The URL of the avatar DriveFile"),
                    avatarBlurhash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true, comment: "The blurhash of the avatar DriveFile"),
                    bannerUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true, comment: "The URL of the banner DriveFile"),
                    bannerBlurhash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true, comment: "The blurhash of the banner DriveFile")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cace4a159ff9f2512dd42373760", x => x.id);
                    table.ForeignKey(
                        name: "FK_58f5c71eaab331645112cf8cfa5",
                        column: x => x.avatarId,
                        principalTable: "drive_file",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_afc64b53f8db3707ceb34eb28e2",
                        column: x => x.bannerId,
                        principalTable: "drive_file",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "drive_folder",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    createdAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "The created date of the DriveFolder."),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false, comment: "The name of the DriveFolder."),
                    userId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true, comment: "The owner ID."),
                    parentId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true, comment: "The parent folder ID. If null, it means the DriveFolder is located in root.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_7a0c089191f5ebdc214e0af808a", x => x.id);
                    table.ForeignKey(
                        name: "FK_00ceffb0cdc238b3233294f08f2",
                        column: x => x.parentId,
                        principalTable: "drive_folder",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_f4fc06e49c0171c85f1c48060d2",
                        column: x => x.userId,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "follow_request",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    createdAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "The created date of the FollowRequest."),
                    followeeId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, comment: "The followee user ID."),
                    followerId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, comment: "The follower user ID."),
                    requestId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true, comment: "id of Follow Activity."),
                    followerHost = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true, comment: "[Denormalized]"),
                    followerInbox = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true, comment: "[Denormalized]"),
                    followerSharedInbox = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true, comment: "[Denormalized]"),
                    followeeHost = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true, comment: "[Denormalized]"),
                    followeeInbox = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true, comment: "[Denormalized]"),
                    followeeSharedInbox = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true, comment: "[Denormalized]")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_53a9aa3725f7a3deb150b39dbfc", x => x.id);
                    table.ForeignKey(
                        name: "FK_12c01c0d1a79f77d9f6c15fadd2",
                        column: x => x.followeeId,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_a7fd92dd6dc519e6fb435dd108f",
                        column: x => x.followerId,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "following",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    createdAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "The created date of the Following."),
                    followeeId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, comment: "The followee user ID."),
                    followerId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, comment: "The follower user ID."),
                    followerHost = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true, comment: "[Denormalized]"),
                    followerInbox = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true, comment: "[Denormalized]"),
                    followerSharedInbox = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true, comment: "[Denormalized]"),
                    followeeHost = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true, comment: "[Denormalized]"),
                    followeeInbox = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true, comment: "[Denormalized]"),
                    followeeSharedInbox = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true, comment: "[Denormalized]")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_c76c6e044bdf76ecf8bfb82a645", x => x.id);
                    table.ForeignKey(
                        name: "FK_24e0042143a18157b234df186c3",
                        column: x => x.followeeId,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_6516c5a6f3c015b4eed39978be5",
                        column: x => x.followerId,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "gallery_post",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    createdAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "The created date of the GalleryPost."),
                    updatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "The updated date of the GalleryPost."),
                    title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    description = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    userId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, comment: "The ID of author."),
                    fileIds = table.Column<List<string>>(type: "character varying(32)[]", nullable: false, defaultValueSql: "'{}'::character varying[]"),
                    isSensitive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Whether the post is sensitive."),
                    likedCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    tags = table.Column<List<string>>(type: "character varying(128)[]", nullable: false, defaultValueSql: "'{}'::character varying[]")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_8e90d7b6015f2c4518881b14753", x => x.id);
                    table.ForeignKey(
                        name: "FK_985b836dddd8615e432d7043ddb",
                        column: x => x.userId,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "html_user_cache_entry",
                columns: table => new
                {
                    userId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    updatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    bio = table.Column<string>(type: "text", nullable: true),
                    fields = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'[]'::jsonb")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_920b9474e3c9cae3f3c37c057e1", x => x.userId);
                    table.ForeignKey(
                        name: "FK_920b9474e3c9cae3f3c37c057e1",
                        column: x => x.userId,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "moderation_log",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    createdAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "The created date of the ModerationLog."),
                    userId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    info = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_d0adca6ecfd068db83e4526cc26", x => x.id);
                    table.ForeignKey(
                        name: "FK_a08ad074601d204e0f69da9a954",
                        column: x => x.userId,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "muting",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    createdAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "The created date of the Muting."),
                    muteeId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, comment: "The mutee user ID."),
                    muterId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, comment: "The muter user ID."),
                    expiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_2e92d06c8b5c602eeb27ca9ba48", x => x.id);
                    table.ForeignKey(
                        name: "FK_93060675b4a79a577f31d260c67",
                        column: x => x.muterId,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ec96b4fed9dae517e0dbbe0675c",
                        column: x => x.muteeId,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "note",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    createdAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "The created date of the Note."),
                    replyId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true, comment: "The ID of reply target."),
                    renoteId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true, comment: "The ID of renote target."),
                    text = table.Column<string>(type: "text", nullable: true),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    cw = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    userId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, comment: "The ID of author."),
                    visibility = table.Column<Note.NoteVisibility>(type: "note_visibility_enum", nullable: false),
                    localOnly = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    renoteCount = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    repliesCount = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    reactions = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
                    uri = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true, comment: "The URI of a note. it will be null when the note is local."),
                    score = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    fileIds = table.Column<List<string>>(type: "character varying(32)[]", nullable: false, defaultValueSql: "'{}'::character varying[]"),
                    attachedFileTypes = table.Column<List<string>>(type: "character varying(256)[]", nullable: false, defaultValueSql: "'{}'::character varying[]"),
                    visibleUserIds = table.Column<List<string>>(type: "character varying(32)[]", nullable: false, defaultValueSql: "'{}'::character varying[]"),
                    mentions = table.Column<List<string>>(type: "character varying(32)[]", nullable: false, defaultValueSql: "'{}'::character varying[]"),
                    mentionedRemoteUsers = table.Column<string>(type: "text", nullable: false, defaultValueSql: "'[]'::text"),
                    emojis = table.Column<List<string>>(type: "character varying(128)[]", nullable: false, defaultValueSql: "'{}'::character varying[]"),
                    tags = table.Column<List<string>>(type: "character varying(128)[]", nullable: false, defaultValueSql: "'{}'::character varying[]"),
                    hasPoll = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    userHost = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true, comment: "[Denormalized]"),
                    replyUserId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true, comment: "[Denormalized]"),
                    replyUserHost = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true, comment: "[Denormalized]"),
                    renoteUserId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true, comment: "[Denormalized]"),
                    renoteUserHost = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true, comment: "[Denormalized]"),
                    url = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true, comment: "The human readable url of a note. it will be null when the note is local."),
                    channelId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true, comment: "The ID of source channel."),
                    threadId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    updatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "The updated date of the Note.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_96d0c172a4fba276b1bbed43058", x => x.id);
                    table.ForeignKey(
                        name: "FK_17cb3553c700a4985dff5a30ff5",
                        column: x => x.replyId,
                        principalTable: "note",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_52ccc804d7c69037d558bac4c96",
                        column: x => x.renoteId,
                        principalTable: "note",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_5b87d9d19127bd5d92026017a7b",
                        column: x => x.userId,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_f22169eb10657bded6d875ac8f9",
                        column: x => x.channelId,
                        principalTable: "channel",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "note_thread_muting",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    createdAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    userId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    threadId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ec5936d94d1a0369646d12a3a47", x => x.id);
                    table.ForeignKey(
                        name: "FK_29c11c7deb06615076f8c95b80a",
                        column: x => x.userId,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "oauth_token",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    createdAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "The created date of the OAuth token"),
                    appId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    userId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, comment: "The auth code for the OAuth token"),
                    token = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, comment: "The OAuth token"),
                    active = table.Column<bool>(type: "boolean", nullable: false, comment: "Whether or not the token has been activated"),
                    scopes = table.Column<List<string>>(type: "character varying(64)[]", nullable: false, comment: "The scopes requested by the OAuth token"),
                    redirectUri = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false, comment: "The redirect URI of the OAuth token")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_7e6a25a3cc4395d1658f5b89c73", x => x.id);
                    table.ForeignKey(
                        name: "FK_6d3ef28ea647b1449ba79690874",
                        column: x => x.appId,
                        principalTable: "oauth_app",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_f6b4b1ac66b753feab5d831ba04",
                        column: x => x.userId,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "page",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    createdAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "The created date of the Page."),
                    updatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "The updated date of the Page."),
                    title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    visibility = table.Column<Page.PageVisibility>(type: "page_visibility_enum", nullable: false),
                    summary = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    alignCenter = table.Column<bool>(type: "boolean", nullable: false),
                    font = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    userId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, comment: "The ID of author."),
                    eyeCatchingImageId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    content = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'[]'::jsonb"),
                    variables = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'[]'::jsonb"),
                    visibleUserIds = table.Column<List<string>>(type: "character varying(32)[]", nullable: false, defaultValueSql: "'{}'::character varying[]"),
                    likedCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    hideTitleWhenPinned = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    script = table.Column<string>(type: "character varying(16384)", maxLength: 16384, nullable: false, defaultValueSql: "''::character varying"),
                    isPublic = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_742f4117e065c5b6ad21b37ba1f", x => x.id);
                    table.ForeignKey(
                        name: "FK_a9ca79ad939bf06066b81c9d3aa",
                        column: x => x.eyeCatchingImageId,
                        principalTable: "drive_file",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ae1d917992dd0c9d9bbdad06c4a",
                        column: x => x.userId,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "password_reset_request",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    createdAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    token = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    userId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fcf4b02eae1403a2edaf87fd074", x => x.id);
                    table.ForeignKey(
                        name: "FK_4bb7fd4a34492ae0e6cc8d30ac8",
                        column: x => x.userId,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "registry_item",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    createdAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "The created date of the RegistryItem."),
                    updatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "The updated date of the RegistryItem."),
                    userId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, comment: "The owner ID."),
                    key = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false, comment: "The key of the RegistryItem."),
                    scope = table.Column<List<string>>(type: "character varying(1024)[]", nullable: false, defaultValueSql: "'{}'::character varying[]"),
                    domain = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    value = table.Column<string>(type: "jsonb", nullable: true, defaultValueSql: "'{}'::jsonb", comment: "The value of the RegistryItem.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_64b3f7e6008b4d89b826cd3af95", x => x.id);
                    table.ForeignKey(
                        name: "FK_fb9d21ba0abb83223263df6bcb3",
                        column: x => x.userId,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "renote_muting",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    createdAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "The created date of the Muting."),
                    muteeId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, comment: "The mutee user ID."),
                    muterId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, comment: "The muter user ID.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_renoteMuting_id", x => x.id);
                    table.ForeignKey(
                        name: "FK_7aa72a5fe76019bfe8e5e0e8b7d",
                        column: x => x.muterId,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_7eac97594bcac5ffcf2068089b6",
                        column: x => x.muteeId,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "session",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    createdAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "The created date of the OAuth token"),
                    userId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    token = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, comment: "The authorization token"),
                    active = table.Column<bool>(type: "boolean", nullable: false, comment: "Whether or not the token has been activated (i.e. 2fa has been confirmed)")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_f55da76ac1c3ac420f444d2ff11", x => x.id);
                    table.ForeignKey(
                        name: "FK_3d2f174ef04fb312fdebd0ddc53",
                        column: x => x.userId,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "signin",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    createdAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "The created date of the Signin."),
                    userId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ip = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    headers = table.Column<string>(type: "jsonb", nullable: false),
                    success = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_9e96ddc025712616fc492b3b588", x => x.id);
                    table.ForeignKey(
                        name: "FK_2c308dbdc50d94dc625670055f7",
                        column: x => x.userId,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sw_subscription",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    createdAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    userId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    endpoint = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    auth = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    publickey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    sendReadMessage = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_e8f763631530051b95eb6279b91", x => x.id);
                    table.ForeignKey(
                        name: "FK_97754ca6f2baff9b4abb7f853dd",
                        column: x => x.userId,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_group",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    createdAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "The created date of the UserGroup."),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    userId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, comment: "The ID of owner."),
                    isPrivate = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_3c29fba6fe013ec8724378ce7c9", x => x.id);
                    table.ForeignKey(
                        name: "FK_3d6b372788ab01be58853003c93",
                        column: x => x.userId,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_keypair",
                columns: table => new
                {
                    userId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    publicKey = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: false),
                    privateKey = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_f4853eb41ab722fe05f81cedeb6", x => x.userId);
                    table.ForeignKey(
                        name: "FK_f4853eb41ab722fe05f81cedeb6",
                        column: x => x.userId,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_list",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    createdAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "The created date of the UserList."),
                    userId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, comment: "The owner ID."),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false, comment: "The name of the UserList."),
                    hideFromHomeTl = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Whether posts from list members should be hidden from the home timeline.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_87bab75775fd9b1ff822b656402", x => x.id);
                    table.ForeignKey(
                        name: "FK_b7fcefbdd1c18dce86687531f99",
                        column: x => x.userId,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_publickey",
                columns: table => new
                {
                    userId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    keyId = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    keyPem = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_10c146e4b39b443ede016f6736d", x => x.userId);
                    table.ForeignKey(
                        name: "FK_10c146e4b39b443ede016f6736d",
                        column: x => x.userId,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_security_key",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying", nullable: false, comment: "Variable-length id given to navigator.credentials.get()"),
                    userId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    publicKey = table.Column<string>(type: "character varying", nullable: false, comment: "Variable-length public key used to verify attestations (hex-encoded)."),
                    lastUsed = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "The date of the last time the UserSecurityKey was successfully validated."),
                    name = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, comment: "User-defined name for this key")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_3e508571121ab39c5f85d10c166", x => x.id);
                    table.ForeignKey(
                        name: "FK_ff9ca3b5f3ee3d0681367a9b447",
                        column: x => x.userId,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "webhook",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    createdAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "The created date of the Antenna."),
                    userId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, comment: "The owner ID."),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false, comment: "The name of the Antenna."),
                    on = table.Column<List<string>>(type: "character varying(128)[]", nullable: false, defaultValueSql: "'{}'::character varying[]"),
                    url = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    secret = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    latestSentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    latestStatus = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_e6765510c2d078db49632b59020", x => x.id);
                    table.ForeignKey(
                        name: "FK_f272c8c8805969e6a6449c77b3c",
                        column: x => x.userId,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "gallery_like",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    createdAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    userId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    postId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_853ab02be39b8de45cd720cc15f", x => x.id);
                    table.ForeignKey(
                        name: "FK_8fd5215095473061855ceb948cf",
                        column: x => x.userId,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_b1cb568bfe569e47b7051699fc8",
                        column: x => x.postId,
                        principalTable: "gallery_post",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "html_note_cache_entry",
                columns: table => new
                {
                    noteId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    updatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    content = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_6ef86ec901b2017cbe82d3a8286", x => x.noteId);
                    table.ForeignKey(
                        name: "FK_6ef86ec901b2017cbe82d3a8286",
                        column: x => x.noteId,
                        principalTable: "note",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "note_edit",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    noteId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, comment: "The ID of note."),
                    text = table.Column<string>(type: "text", nullable: true),
                    cw = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    fileIds = table.Column<List<string>>(type: "character varying(32)[]", nullable: false, defaultValueSql: "'{}'::character varying[]"),
                    updatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "The updated date of the Note.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_736fc6e0d4e222ecc6f82058e08", x => x.id);
                    table.ForeignKey(
                        name: "FK_702ad5ae993a672e4fbffbcd38c",
                        column: x => x.noteId,
                        principalTable: "note",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "note_favorite",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    createdAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "The created date of the NoteFavorite."),
                    userId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    noteId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_af0da35a60b9fa4463a62082b36", x => x.id);
                    table.ForeignKey(
                        name: "FK_0e00498f180193423c992bc4370",
                        column: x => x.noteId,
                        principalTable: "note",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_47f4b1892f5d6ba8efb3057d81a",
                        column: x => x.userId,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "note_reaction",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    createdAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "The created date of the NoteReaction."),
                    userId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    noteId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    reaction = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_767ec729b108799b587a3fcc9cf", x => x.id);
                    table.ForeignKey(
                        name: "FK_13761f64257f40c5636d0ff95ee",
                        column: x => x.userId,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_45145e4953780f3cd5656f0ea6a",
                        column: x => x.noteId,
                        principalTable: "note",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "note_unread",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    userId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    noteId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    noteUserId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, comment: "[Denormalized]"),
                    isSpecified = table.Column<bool>(type: "boolean", nullable: false),
                    isMentioned = table.Column<bool>(type: "boolean", nullable: false),
                    noteChannelId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true, comment: "[Denormalized]")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_1904eda61a784f57e6e51fa9c1f", x => x.id);
                    table.ForeignKey(
                        name: "FK_56b0166d34ddae49d8ef7610bb9",
                        column: x => x.userId,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_e637cba4dc4410218c4251260e4",
                        column: x => x.noteId,
                        principalTable: "note",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "note_watching",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    createdAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "The created date of the NoteWatching."),
                    userId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, comment: "The watcher ID."),
                    noteId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, comment: "The target Note ID."),
                    noteUserId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, comment: "[Denormalized]")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_49286fdb23725945a74aa27d757", x => x.id);
                    table.ForeignKey(
                        name: "FK_03e7028ab8388a3f5e3ce2a8619",
                        column: x => x.noteId,
                        principalTable: "note",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_b0134ec406e8d09a540f8182888",
                        column: x => x.userId,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "poll",
                columns: table => new
                {
                    noteId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    expiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    multiple = table.Column<bool>(type: "boolean", nullable: false),
                    choices = table.Column<List<string>>(type: "character varying(256)[]", nullable: false, defaultValueSql: "'{}'::character varying[]"),
                    votes = table.Column<List<int>>(type: "integer[]", nullable: false),
                    userId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, comment: "[Denormalized]"),
                    userHost = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true, comment: "[Denormalized]"),
                    noteVisibility = table.Column<LegacyModels.PollNoteVisibility>(type: "poll_notevisibility_enum", nullable: false, comment: "[Denormalized]")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_da851e06d0dfe2ef397d8b1bf1b", x => x.noteId);
                    table.ForeignKey(
                        name: "FK_da851e06d0dfe2ef397d8b1bf1b",
                        column: x => x.noteId,
                        principalTable: "note",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "poll_vote",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    createdAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "The created date of the PollVote."),
                    userId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    noteId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    choice = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fd002d371201c472490ba89c6a0", x => x.id);
                    table.ForeignKey(
                        name: "FK_66d2bd2ee31d14bcc23069a89f8",
                        column: x => x.userId,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_aecfbd5ef60374918e63ee95fa7",
                        column: x => x.noteId,
                        principalTable: "note",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "promo_note",
                columns: table => new
                {
                    noteId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    expiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    userId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, comment: "[Denormalized]")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_e263909ca4fe5d57f8d4230dd5c", x => x.noteId);
                    table.ForeignKey(
                        name: "FK_e263909ca4fe5d57f8d4230dd5c",
                        column: x => x.noteId,
                        principalTable: "note",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "promo_read",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    createdAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "The created date of the PromoRead."),
                    userId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    noteId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_61917c1541002422b703318b7c9", x => x.id);
                    table.ForeignKey(
                        name: "FK_9657d55550c3d37bfafaf7d4b05",
                        column: x => x.userId,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_a46a1a603ecee695d7db26da5f4",
                        column: x => x.noteId,
                        principalTable: "note",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_note_pining",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    createdAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "The created date of the UserNotePinings."),
                    userId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    noteId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_a6a2dad4ae000abce2ea9d9b103", x => x.id);
                    table.ForeignKey(
                        name: "FK_68881008f7c3588ad7ecae471cf",
                        column: x => x.noteId,
                        principalTable: "note",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_bfbc6f79ba4007b4ce5097f08d6",
                        column: x => x.userId,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "page_like",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    createdAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    userId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    pageId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_813f034843af992d3ae0f43c64c", x => x.id);
                    table.ForeignKey(
                        name: "FK_0e61efab7f88dbb79c9166dbb48",
                        column: x => x.userId,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_cf8782626dced3176038176a847",
                        column: x => x.pageId,
                        principalTable: "page",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_profile",
                columns: table => new
                {
                    userId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    location = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true, comment: "The location of the User."),
                    birthday = table.Column<string>(type: "character(10)", fixedLength: true, maxLength: 10, nullable: true, comment: "The birthday (YYYY-MM-DD) of the User."),
                    description = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true, comment: "The description (bio) of the User."),
                    fields = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'[]'::jsonb"),
                    url = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true, comment: "Remote URL of the user."),
                    ffVisibility = table.Column<UserProfile.UserProfileFFVisibility>(type: "user_profile_ffvisibility_enum", nullable: false, defaultValue: UserProfile.UserProfileFFVisibility.Public),
                    mutingNotificationTypes = table.Column<List<LegacyModels.MutingNotificationType>>(type: "user_profile_mutingnotificationtypes_enum[]", nullable: false, defaultValueSql: "'{}'::public.user_profile_mutingnotificationtypes_enum[]"),
                    email = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true, comment: "The email address of the User."),
                    emailVerifyCode = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    emailVerified = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    twoFactorTempSecret = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    twoFactorSecret = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    twoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    password = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true, comment: "The password hash of the User. It will be null if the origin of the user is local."),
                    clientData = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb", comment: "The client-specific data of the User."),
                    autoAcceptFollowed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    alwaysMarkNsfw = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    carefulBot = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    userHost = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true, comment: "[Denormalized]"),
                    securityKeysAvailable = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    usePasswordLessLogin = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    pinnedPageId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    room = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb", comment: "The room data of the User."),
                    integrations = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
                    injectFeaturedNote = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    enableWordMute = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    mutedWords = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'[]'::jsonb"),
                    noCrawle = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Whether reject index by crawler."),
                    receiveAnnouncementEmail = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    emailNotificationTypes = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'[\"follow\", \"receiveFollowRequest\", \"groupInvited\"]'::jsonb"),
                    lang = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    mutedInstances = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'[]'::jsonb", comment: "List of instances muted by the user."),
                    publicReactions = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    moderationNote = table.Column<string>(type: "character varying(8192)", maxLength: 8192, nullable: false, defaultValueSql: "''::character varying"),
                    preventAiLearning = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    mentions = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'[]'::jsonb")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_51cb79b5555effaf7d69ba1cff9", x => x.userId);
                    table.ForeignKey(
                        name: "FK_51cb79b5555effaf7d69ba1cff9",
                        column: x => x.userId,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_6dc44f1ceb65b1e72bacef2ca27",
                        column: x => x.pinnedPageId,
                        principalTable: "page",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "messaging_message",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    createdAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "The created date of the MessagingMessage."),
                    userId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, comment: "The sender user ID."),
                    recipientId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true, comment: "The recipient user ID."),
                    text = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: true),
                    isRead = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    fileId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    groupId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true, comment: "The recipient group ID."),
                    reads = table.Column<List<string>>(type: "character varying(32)[]", nullable: false, defaultValueSql: "'{}'::character varying[]"),
                    uri = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_db398fd79dc95d0eb8c30456eaa", x => x.id);
                    table.ForeignKey(
                        name: "FK_2c4be03b446884f9e9c502135be",
                        column: x => x.groupId,
                        principalTable: "user_group",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_535def119223ac05ad3fa9ef64b",
                        column: x => x.fileId,
                        principalTable: "drive_file",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_5377c307783fce2b6d352e1203b",
                        column: x => x.userId,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_cac14a4e3944454a5ce7daa5142",
                        column: x => x.recipientId,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_group_invitation",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    createdAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "The created date of the UserGroupInvitation."),
                    userId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, comment: "The user ID."),
                    userGroupId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, comment: "The group ID.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_160c63ec02bf23f6a5c5e8140d6", x => x.id);
                    table.ForeignKey(
                        name: "FK_5cc8c468090e129857e9fecce5a",
                        column: x => x.userGroupId,
                        principalTable: "user_group",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_bfbc6305547539369fe73eb144a",
                        column: x => x.userId,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_group_invite",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    createdAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    userId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    userGroupId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_3893884af0d3a5f4d01e7921a97", x => x.id);
                    table.ForeignKey(
                        name: "FK_1039988afa3bf991185b277fe03",
                        column: x => x.userId,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_e10924607d058004304611a436a",
                        column: x => x.userGroupId,
                        principalTable: "user_group",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_group_joining",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    createdAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "The created date of the UserGroupJoining."),
                    userId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, comment: "The user ID."),
                    userGroupId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, comment: "The group ID.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_15f2425885253c5507e1599cfe7", x => x.id);
                    table.ForeignKey(
                        name: "FK_67dc758bc0566985d1b3d399865",
                        column: x => x.userGroupId,
                        principalTable: "user_group",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_f3a1b4bd0c7cabba958a0c0b231",
                        column: x => x.userId,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_list_joining",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    createdAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "The created date of the UserListJoining."),
                    userId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, comment: "The user ID."),
                    userListId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, comment: "The list ID.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_11abb3768da1c5f8de101c9df45", x => x.id);
                    table.ForeignKey(
                        name: "FK_605472305f26818cc93d1baaa74",
                        column: x => x.userListId,
                        principalTable: "user_list",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_d844bfc6f3f523a05189076efaa",
                        column: x => x.userId,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "notification",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    createdAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "The created date of the Notification."),
                    notifieeId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, comment: "The ID of recipient user of the Notification."),
                    notifierId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true, comment: "The ID of sender user of the Notification."),
                    isRead = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Whether the notification was read."),
                    type = table.Column<Notification.NotificationType>(type: "notification_type_enum", nullable: false, comment: "The type of the Notification."),
                    noteId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    reaction = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    choice = table.Column<int>(type: "integer", nullable: true),
                    followRequestId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    userGroupInvitationId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    customBody = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    customHeader = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    customIcon = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    appAccessTokenId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_705b6c7cdf9b2c2ff7ac7872cb7", x => x.id);
                    table.ForeignKey(
                        name: "FK_3b4e96eec8d36a8bbb9d02aa710",
                        column: x => x.notifierId,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_3c601b70a1066d2c8b517094cb9",
                        column: x => x.notifieeId,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_769cb6b73a1efe22ddf733ac453",
                        column: x => x.noteId,
                        principalTable: "note",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_8fe87814e978053a53b1beb7e98",
                        column: x => x.userGroupInvitationId,
                        principalTable: "user_group_invitation",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_bd7fab507621e635b32cd31892c",
                        column: x => x.followRequestId,
                        principalTable: "follow_request",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_e22bf6bda77b6adc1fd9e75c8c9",
                        column: x => x.appAccessTokenId,
                        principalTable: "access_token",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IDX_04cc96756f89d0b7f9473e8cdf",
                table: "abuse_user_report",
                column: "reporterId");

            migrationBuilder.CreateIndex(
                name: "IDX_2b15aaf4a0dc5be3499af7ab6a",
                table: "abuse_user_report",
                column: "resolved");

            migrationBuilder.CreateIndex(
                name: "IDX_4ebbf7f93cdc10e8d1ef2fc6cd",
                table: "abuse_user_report",
                column: "targetUserHost");

            migrationBuilder.CreateIndex(
                name: "IDX_a9021cc2e1feb5f72d3db6e9f5",
                table: "abuse_user_report",
                column: "targetUserId");

            migrationBuilder.CreateIndex(
                name: "IDX_db2098070b2b5a523c58181f74",
                table: "abuse_user_report",
                column: "createdAt");

            migrationBuilder.CreateIndex(
                name: "IDX_f8d8b93740ad12c4ce8213a199",
                table: "abuse_user_report",
                column: "reporterHost");

            migrationBuilder.CreateIndex(
                name: "IX_abuse_user_report_assigneeId",
                table: "abuse_user_report",
                column: "assigneeId");

            migrationBuilder.CreateIndex(
                name: "IDX_64c327441248bae40f7d92f34f",
                table: "access_token",
                column: "hash");

            migrationBuilder.CreateIndex(
                name: "IDX_70ba8f6af34bc924fc9e12adb8",
                table: "access_token",
                column: "token");

            migrationBuilder.CreateIndex(
                name: "IDX_9949557d0e1b2c19e5344c171e",
                table: "access_token",
                column: "userId");

            migrationBuilder.CreateIndex(
                name: "IDX_bf3a053c07d9fb5d87317c56ee",
                table: "access_token",
                column: "session");

            migrationBuilder.CreateIndex(
                name: "IX_access_token_appId",
                table: "access_token",
                column: "appId");

            migrationBuilder.CreateIndex(
                name: "IDX_118ec703e596086fc4515acb39",
                table: "announcement",
                column: "createdAt");

            migrationBuilder.CreateIndex(
                name: "IDX_603a7b1e7aa0533c6c88e9bfaf",
                table: "announcement_read",
                column: "announcementId");

            migrationBuilder.CreateIndex(
                name: "IDX_8288151386172b8109f7239ab2",
                table: "announcement_read",
                column: "userId");

            migrationBuilder.CreateIndex(
                name: "IDX_924fa71815cfa3941d003702a0",
                table: "announcement_read",
                columns: new[] { "userId", "announcementId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IDX_6446c571a0e8d0f05f01c78909",
                table: "antenna",
                column: "userId");

            migrationBuilder.CreateIndex(
                name: "IX_antenna_userGroupJoiningId",
                table: "antenna",
                column: "userGroupJoiningId");

            migrationBuilder.CreateIndex(
                name: "IX_antenna_userListId",
                table: "antenna",
                column: "userListId");

            migrationBuilder.CreateIndex(
                name: "IDX_048a757923ed8b157e9895da53",
                table: "app",
                column: "createdAt");

            migrationBuilder.CreateIndex(
                name: "IDX_3f5b0899ef90527a3462d7c2cb",
                table: "app",
                column: "userId");

            migrationBuilder.CreateIndex(
                name: "IDX_f49922d511d666848f250663c4",
                table: "app",
                column: "secret");

            migrationBuilder.CreateIndex(
                name: "IDX_47efb914aed1f72dd39a306c7b",
                table: "attestation_challenge",
                column: "challenge");

            migrationBuilder.CreateIndex(
                name: "IDX_f1a461a618fa1755692d0e0d59",
                table: "attestation_challenge",
                column: "userId");

            migrationBuilder.CreateIndex(
                name: "IDX_62cb09e1129f6ec024ef66e183",
                table: "auth_session",
                column: "token");

            migrationBuilder.CreateIndex(
                name: "IX_auth_session_appId",
                table: "auth_session",
                column: "appId");

            migrationBuilder.CreateIndex(
                name: "IX_auth_session_userId",
                table: "auth_session",
                column: "userId");

            migrationBuilder.CreateIndex(
                name: "IDX_0627125f1a8a42c9a1929edb55",
                table: "blocking",
                column: "blockerId");

            migrationBuilder.CreateIndex(
                name: "IDX_2cd4a2743a99671308f5417759",
                table: "blocking",
                column: "blockeeId");

            migrationBuilder.CreateIndex(
                name: "IDX_98a1bc5cb30dfd159de056549f",
                table: "blocking",
                columns: new[] { "blockerId", "blockeeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IDX_b9a354f7941c1e779f3b33aea6",
                table: "blocking",
                column: "createdAt");

            migrationBuilder.CreateIndex(
                name: "IDX_094b86cd36bb805d1aa1e8cc9a",
                table: "channel",
                column: "usersCount");

            migrationBuilder.CreateIndex(
                name: "IDX_0f58c11241e649d2a638a8de94",
                table: "channel",
                column: "notesCount");

            migrationBuilder.CreateIndex(
                name: "IDX_29ef80c6f13bcea998447fce43",
                table: "channel",
                column: "lastNotedAt");

            migrationBuilder.CreateIndex(
                name: "IDX_71cb7b435b7c0d4843317e7e16",
                table: "channel",
                column: "createdAt");

            migrationBuilder.CreateIndex(
                name: "IDX_823bae55bd81b3be6e05cff438",
                table: "channel",
                column: "userId");

            migrationBuilder.CreateIndex(
                name: "IX_channel_bannerId",
                table: "channel",
                column: "bannerId");

            migrationBuilder.CreateIndex(
                name: "IDX_0e43068c3f92cab197c3d3cd86",
                table: "channel_following",
                column: "followeeId");

            migrationBuilder.CreateIndex(
                name: "IDX_11e71f2511589dcc8a4d3214f9",
                table: "channel_following",
                column: "createdAt");

            migrationBuilder.CreateIndex(
                name: "IDX_2e230dd45a10e671d781d99f3e",
                table: "channel_following",
                columns: new[] { "followerId", "followeeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IDX_6d8084ec9496e7334a4602707e",
                table: "channel_following",
                column: "followerId");

            migrationBuilder.CreateIndex(
                name: "IDX_8125f950afd3093acb10d2db8a",
                table: "channel_note_pining",
                column: "channelId");

            migrationBuilder.CreateIndex(
                name: "IDX_f36fed37d6d4cdcc68c803cd9c",
                table: "channel_note_pining",
                columns: new[] { "channelId", "noteId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_channel_note_pining_noteId",
                table: "channel_note_pining",
                column: "noteId");

            migrationBuilder.CreateIndex(
                name: "IDX_2b5ec6c574d6802c94c80313fb",
                table: "clip",
                column: "userId");

            migrationBuilder.CreateIndex(
                name: "IDX_6fc0ec357d55a18646262fdfff",
                table: "clip_note",
                columns: new[] { "noteId", "clipId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IDX_a012eaf5c87c65da1deb5fdbfa",
                table: "clip_note",
                column: "noteId");

            migrationBuilder.CreateIndex(
                name: "IDX_ebe99317bbbe9968a0c6f579ad",
                table: "clip_note",
                column: "clipId");

            migrationBuilder.CreateIndex(
                name: "IDX_315c779174fe8247ab324f036e",
                table: "drive_file",
                column: "isLink");

            migrationBuilder.CreateIndex(
                name: "IDX_37bb9a1b4585f8a3beb24c62d6",
                table: "drive_file",
                column: "md5");

            migrationBuilder.CreateIndex(
                name: "IDX_55720b33a61a7c806a8215b825",
                table: "drive_file",
                columns: new[] { "userId", "folderId", "id" });

            migrationBuilder.CreateIndex(
                name: "IDX_860fa6f6c7df5bb887249fba22",
                table: "drive_file",
                column: "userId");

            migrationBuilder.CreateIndex(
                name: "IDX_92779627994ac79277f070c91e",
                table: "drive_file",
                column: "userHost");

            migrationBuilder.CreateIndex(
                name: "IDX_a40b8df8c989d7db937ea27cf6",
                table: "drive_file",
                column: "type");

            migrationBuilder.CreateIndex(
                name: "IDX_a7eba67f8b3fa27271e85d2e26",
                table: "drive_file",
                column: "isSensitive");

            migrationBuilder.CreateIndex(
                name: "IDX_bb90d1956dafc4068c28aa7560",
                table: "drive_file",
                column: "folderId");

            migrationBuilder.CreateIndex(
                name: "IDX_c55b2b7c284d9fef98026fc88e",
                table: "drive_file",
                column: "webpublicAccessKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IDX_c8dfad3b72196dd1d6b5db168a",
                table: "drive_file",
                column: "createdAt");

            migrationBuilder.CreateIndex(
                name: "IDX_d85a184c2540d2deba33daf642",
                table: "drive_file",
                column: "accessKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IDX_e5848eac4940934e23dbc17581",
                table: "drive_file",
                column: "uri");

            migrationBuilder.CreateIndex(
                name: "IDX_e74022ce9a074b3866f70e0d27",
                table: "drive_file",
                column: "thumbnailAccessKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IDX_00ceffb0cdc238b3233294f08f",
                table: "drive_folder",
                column: "parentId");

            migrationBuilder.CreateIndex(
                name: "IDX_02878d441ceae15ce060b73daf",
                table: "drive_folder",
                column: "createdAt");

            migrationBuilder.CreateIndex(
                name: "IDX_f4fc06e49c0171c85f1c48060d",
                table: "drive_folder",
                column: "userId");

            migrationBuilder.CreateIndex(
                name: "IDX_4f4d35e1256c84ae3d1f0eab10",
                table: "emoji",
                columns: new[] { "name", "host" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IDX_5900e907bb46516ddf2871327c",
                table: "emoji",
                column: "host");

            migrationBuilder.CreateIndex(
                name: "IDX_b37dafc86e9af007e3295c2781",
                table: "emoji",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "IDX_12c01c0d1a79f77d9f6c15fadd",
                table: "follow_request",
                column: "followeeId");

            migrationBuilder.CreateIndex(
                name: "IDX_a7fd92dd6dc519e6fb435dd108",
                table: "follow_request",
                column: "followerId");

            migrationBuilder.CreateIndex(
                name: "IDX_d54a512b822fac7ed52800f6b4",
                table: "follow_request",
                columns: new[] { "followerId", "followeeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IDX_24e0042143a18157b234df186c",
                table: "following",
                column: "followeeId");

            migrationBuilder.CreateIndex(
                name: "IDX_307be5f1d1252e0388662acb96",
                table: "following",
                columns: new[] { "followerId", "followeeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IDX_4ccd2239268ebbd1b35e318754",
                table: "following",
                column: "followerHost");

            migrationBuilder.CreateIndex(
                name: "IDX_582f8fab771a9040a12961f3e7",
                table: "following",
                column: "createdAt");

            migrationBuilder.CreateIndex(
                name: "IDX_6516c5a6f3c015b4eed39978be",
                table: "following",
                column: "followerId");

            migrationBuilder.CreateIndex(
                name: "IDX_fcdafee716dfe9c3b5fde90f30",
                table: "following",
                column: "followeeHost");

            migrationBuilder.CreateIndex(
                name: "IDX_8fd5215095473061855ceb948c",
                table: "gallery_like",
                column: "userId");

            migrationBuilder.CreateIndex(
                name: "IDX_df1b5f4099e99fb0bc5eae53b6",
                table: "gallery_like",
                columns: new[] { "userId", "postId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_gallery_like_postId",
                table: "gallery_like",
                column: "postId");

            migrationBuilder.CreateIndex(
                name: "IDX_05cca34b985d1b8edc1d1e28df",
                table: "gallery_post",
                column: "tags");

            migrationBuilder.CreateIndex(
                name: "IDX_1a165c68a49d08f11caffbd206",
                table: "gallery_post",
                column: "likedCount");

            migrationBuilder.CreateIndex(
                name: "IDX_3ca50563facd913c425e7a89ee",
                table: "gallery_post",
                column: "fileIds");

            migrationBuilder.CreateIndex(
                name: "IDX_8f1a239bd077c8864a20c62c2c",
                table: "gallery_post",
                column: "createdAt");

            migrationBuilder.CreateIndex(
                name: "IDX_985b836dddd8615e432d7043dd",
                table: "gallery_post",
                column: "userId");

            migrationBuilder.CreateIndex(
                name: "IDX_f2d744d9a14d0dfb8b96cb7fc5",
                table: "gallery_post",
                column: "isSensitive");

            migrationBuilder.CreateIndex(
                name: "IDX_f631d37835adb04792e361807c",
                table: "gallery_post",
                column: "updatedAt");

            migrationBuilder.CreateIndex(
                name: "IDX_0b03cbcd7e6a7ce068efa8ecc2",
                table: "hashtag",
                column: "attachedRemoteUsersCount");

            migrationBuilder.CreateIndex(
                name: "IDX_0c44bf4f680964145f2a68a341",
                table: "hashtag",
                column: "attachedLocalUsersCount");

            migrationBuilder.CreateIndex(
                name: "IDX_0e206cec573f1edff4a3062923",
                table: "hashtag",
                column: "mentionedLocalUsersCount");

            migrationBuilder.CreateIndex(
                name: "IDX_2710a55f826ee236ea1a62698f",
                table: "hashtag",
                column: "mentionedUsersCount");

            migrationBuilder.CreateIndex(
                name: "IDX_347fec870eafea7b26c8a73bac",
                table: "hashtag",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IDX_4c02d38a976c3ae132228c6fce",
                table: "hashtag",
                column: "mentionedRemoteUsersCount");

            migrationBuilder.CreateIndex(
                name: "IDX_d57f9030cd3af7f63ffb1c267c",
                table: "hashtag",
                column: "attachedUsersCount");

            migrationBuilder.CreateIndex(
                name: "IDX_2cd3b2a6b4cf0b910b260afe08",
                table: "instance",
                column: "caughtAt");

            migrationBuilder.CreateIndex(
                name: "IDX_34500da2e38ac393f7bb6b299c",
                table: "instance",
                column: "isSuspended");

            migrationBuilder.CreateIndex(
                name: "IDX_8d5afc98982185799b160e10eb",
                table: "instance",
                column: "host",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IDX_2c4be03b446884f9e9c502135b",
                table: "messaging_message",
                column: "groupId");

            migrationBuilder.CreateIndex(
                name: "IDX_5377c307783fce2b6d352e1203",
                table: "messaging_message",
                column: "userId");

            migrationBuilder.CreateIndex(
                name: "IDX_cac14a4e3944454a5ce7daa514",
                table: "messaging_message",
                column: "recipientId");

            migrationBuilder.CreateIndex(
                name: "IDX_e21cd3646e52ef9c94aaf17c2e",
                table: "messaging_message",
                column: "createdAt");

            migrationBuilder.CreateIndex(
                name: "IX_messaging_message_fileId",
                table: "messaging_message",
                column: "fileId");

            migrationBuilder.CreateIndex(
                name: "IDX_a08ad074601d204e0f69da9a95",
                table: "moderation_log",
                column: "userId");

            migrationBuilder.CreateIndex(
                name: "IDX_1eb9d9824a630321a29fd3b290",
                table: "muting",
                columns: new[] { "muterId", "muteeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IDX_93060675b4a79a577f31d260c6",
                table: "muting",
                column: "muterId");

            migrationBuilder.CreateIndex(
                name: "IDX_c1fd1c3dfb0627aa36c253fd14",
                table: "muting",
                column: "expiresAt");

            migrationBuilder.CreateIndex(
                name: "IDX_ec96b4fed9dae517e0dbbe0675",
                table: "muting",
                column: "muteeId");

            migrationBuilder.CreateIndex(
                name: "IDX_f86d57fbca33c7a4e6897490cc",
                table: "muting",
                column: "createdAt");

            migrationBuilder.CreateIndex(
                name: "IDX_153536c67d05e9adb24e99fc2b",
                table: "note",
                column: "uri",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IDX_17cb3553c700a4985dff5a30ff",
                table: "note",
                column: "replyId");

            migrationBuilder.CreateIndex(
                name: "IDX_25dfc71b0369b003a4cd434d0b",
                table: "note",
                column: "attachedFileTypes");

            migrationBuilder.CreateIndex(
                name: "IDX_51c063b6a133a9cb87145450f5",
                table: "note",
                column: "fileIds");

            migrationBuilder.CreateIndex(
                name: "IDX_52ccc804d7c69037d558bac4c9",
                table: "note",
                column: "renoteId");

            migrationBuilder.CreateIndex(
                name: "IDX_54ebcb6d27222913b908d56fd8",
                table: "note",
                column: "mentions");

            migrationBuilder.CreateIndex(
                name: "IDX_5b87d9d19127bd5d92026017a7",
                table: "note",
                column: "userId");

            migrationBuilder.CreateIndex(
                name: "IDX_7125a826ab192eb27e11d358a5",
                table: "note",
                column: "userHost");

            migrationBuilder.CreateIndex(
                name: "IDX_796a8c03959361f97dc2be1d5c",
                table: "note",
                column: "visibleUserIds");

            migrationBuilder.CreateIndex(
                name: "IDX_88937d94d7443d9a99a76fa5c0",
                table: "note",
                column: "tags");

            migrationBuilder.CreateIndex(
                name: "IDX_NOTE_MENTIONS",
                table: "note",
                column: "mentions")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IDX_NOTE_TAGS",
                table: "note",
                column: "tags")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IDX_NOTE_VISIBLE_USER_IDS",
                table: "note",
                column: "visibleUserIds")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IDX_d4ebdef929896d6dc4a3c5bb48",
                table: "note",
                column: "threadId");

            migrationBuilder.CreateIndex(
                name: "IDX_e7c0567f5261063592f022e9b5",
                table: "note",
                column: "createdAt");

            migrationBuilder.CreateIndex(
                name: "IDX_f22169eb10657bded6d875ac8f",
                table: "note",
                column: "channelId");

            migrationBuilder.CreateIndex(
                name: "IDX_note_createdAt_userId",
                table: "note",
                columns: new[] { "createdAt", "userId" });

            migrationBuilder.CreateIndex(
                name: "IDX_note_id_userHost",
                table: "note",
                columns: new[] { "id", "userHost" });

            migrationBuilder.CreateIndex(
                name: "IDX_note_url",
                table: "note",
                column: "url");

            migrationBuilder.CreateIndex(
                name: "IDX_note_userId_id",
                table: "note",
                columns: new[] { "userId", "id" });

            migrationBuilder.CreateIndex(
                name: "note_text_fts_idx",
                table: "note",
                column: "text")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IDX_702ad5ae993a672e4fbffbcd38",
                table: "note_edit",
                column: "noteId");

            migrationBuilder.CreateIndex(
                name: "IDX_0f4fb9ad355f3effff221ef245",
                table: "note_favorite",
                columns: new[] { "userId", "noteId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IDX_47f4b1892f5d6ba8efb3057d81",
                table: "note_favorite",
                column: "userId");

            migrationBuilder.CreateIndex(
                name: "IX_note_favorite_noteId",
                table: "note_favorite",
                column: "noteId");

            migrationBuilder.CreateIndex(
                name: "IDX_01f4581f114e0ebd2bbb876f0b",
                table: "note_reaction",
                column: "createdAt");

            migrationBuilder.CreateIndex(
                name: "IDX_13761f64257f40c5636d0ff95e",
                table: "note_reaction",
                column: "userId");

            migrationBuilder.CreateIndex(
                name: "IDX_45145e4953780f3cd5656f0ea6",
                table: "note_reaction",
                column: "noteId");

            migrationBuilder.CreateIndex(
                name: "IDX_ad0c221b25672daf2df320a817",
                table: "note_reaction",
                columns: new[] { "userId", "noteId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IDX_29c11c7deb06615076f8c95b80",
                table: "note_thread_muting",
                column: "userId");

            migrationBuilder.CreateIndex(
                name: "IDX_ae7aab18a2641d3e5f25e0c4ea",
                table: "note_thread_muting",
                columns: new[] { "userId", "threadId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IDX_c426394644267453e76f036926",
                table: "note_thread_muting",
                column: "threadId");

            migrationBuilder.CreateIndex(
                name: "IDX_25b1dd384bec391b07b74b861c",
                table: "note_unread",
                column: "isMentioned");

            migrationBuilder.CreateIndex(
                name: "IDX_29e8c1d579af54d4232939f994",
                table: "note_unread",
                column: "noteUserId");

            migrationBuilder.CreateIndex(
                name: "IDX_56b0166d34ddae49d8ef7610bb",
                table: "note_unread",
                column: "userId");

            migrationBuilder.CreateIndex(
                name: "IDX_6a57f051d82c6d4036c141e107",
                table: "note_unread",
                column: "noteChannelId");

            migrationBuilder.CreateIndex(
                name: "IDX_89a29c9237b8c3b6b3cbb4cb30",
                table: "note_unread",
                column: "isSpecified");

            migrationBuilder.CreateIndex(
                name: "IDX_d908433a4953cc13216cd9c274",
                table: "note_unread",
                columns: new[] { "userId", "noteId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IDX_e637cba4dc4410218c4251260e",
                table: "note_unread",
                column: "noteId");

            migrationBuilder.CreateIndex(
                name: "IDX_03e7028ab8388a3f5e3ce2a861",
                table: "note_watching",
                column: "noteId");

            migrationBuilder.CreateIndex(
                name: "IDX_318cdf42a9cfc11f479bd802bb",
                table: "note_watching",
                column: "createdAt");

            migrationBuilder.CreateIndex(
                name: "IDX_44499765eec6b5489d72c4253b",
                table: "note_watching",
                column: "noteUserId");

            migrationBuilder.CreateIndex(
                name: "IDX_a42c93c69989ce1d09959df4cf",
                table: "note_watching",
                columns: new[] { "userId", "noteId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IDX_b0134ec406e8d09a540f818288",
                table: "note_watching",
                column: "userId");

            migrationBuilder.CreateIndex(
                name: "IDX_080ab397c379af09b9d2169e5b",
                table: "notification",
                column: "isRead");

            migrationBuilder.CreateIndex(
                name: "IDX_33f33cc8ef29d805a97ff4628b",
                table: "notification",
                column: "type");

            migrationBuilder.CreateIndex(
                name: "IDX_3b4e96eec8d36a8bbb9d02aa71",
                table: "notification",
                column: "notifierId");

            migrationBuilder.CreateIndex(
                name: "IDX_3c601b70a1066d2c8b517094cb",
                table: "notification",
                column: "notifieeId");

            migrationBuilder.CreateIndex(
                name: "IDX_b11a5e627c41d4dc3170f1d370",
                table: "notification",
                column: "createdAt");

            migrationBuilder.CreateIndex(
                name: "IDX_e22bf6bda77b6adc1fd9e75c8c",
                table: "notification",
                column: "appAccessTokenId");

            migrationBuilder.CreateIndex(
                name: "IX_notification_followRequestId",
                table: "notification",
                column: "followRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_notification_noteId",
                table: "notification",
                column: "noteId");

            migrationBuilder.CreateIndex(
                name: "IX_notification_userGroupInvitationId",
                table: "notification",
                column: "userGroupInvitationId");

            migrationBuilder.CreateIndex(
                name: "IDX_65b61f406c811241e1315a2f82",
                table: "oauth_app",
                column: "clientId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IDX_2cbeb4b389444bcf4379ef4273",
                table: "oauth_token",
                column: "token");

            migrationBuilder.CreateIndex(
                name: "IDX_dc5fe174a8b59025055f0ec136",
                table: "oauth_token",
                column: "code");

            migrationBuilder.CreateIndex(
                name: "IX_oauth_token_appId",
                table: "oauth_token",
                column: "appId");

            migrationBuilder.CreateIndex(
                name: "IX_oauth_token_userId",
                table: "oauth_token",
                column: "userId");

            migrationBuilder.CreateIndex(
                name: "IDX_2133ef8317e4bdb839c0dcbf13",
                table: "page",
                columns: new[] { "userId", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IDX_90148bbc2bf0854428786bfc15",
                table: "page",
                column: "visibleUserIds");

            migrationBuilder.CreateIndex(
                name: "IDX_ae1d917992dd0c9d9bbdad06c4",
                table: "page",
                column: "userId");

            migrationBuilder.CreateIndex(
                name: "IDX_af639b066dfbca78b01a920f8a",
                table: "page",
                column: "updatedAt");

            migrationBuilder.CreateIndex(
                name: "IDX_b82c19c08afb292de4600d99e4",
                table: "page",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "IDX_fbb4297c927a9b85e9cefa2eb1",
                table: "page",
                column: "createdAt");

            migrationBuilder.CreateIndex(
                name: "IX_page_eyeCatchingImageId",
                table: "page",
                column: "eyeCatchingImageId");

            migrationBuilder.CreateIndex(
                name: "IDX_0e61efab7f88dbb79c9166dbb4",
                table: "page_like",
                column: "userId");

            migrationBuilder.CreateIndex(
                name: "IDX_4ce6fb9c70529b4c8ac46c9bfa",
                table: "page_like",
                columns: new[] { "userId", "pageId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_page_like_pageId",
                table: "page_like",
                column: "pageId");

            migrationBuilder.CreateIndex(
                name: "IDX_0b575fa9a4cfe638a925949285",
                table: "password_reset_request",
                column: "token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IDX_4bb7fd4a34492ae0e6cc8d30ac",
                table: "password_reset_request",
                column: "userId");

            migrationBuilder.CreateIndex(
                name: "IDX_0610ebcfcfb4a18441a9bcdab2",
                table: "poll",
                column: "userId");

            migrationBuilder.CreateIndex(
                name: "IDX_7fa20a12319c7f6dc3aed98c0a",
                table: "poll",
                column: "userHost");

            migrationBuilder.CreateIndex(
                name: "IDX_0fb627e1c2f753262a74f0562d",
                table: "poll_vote",
                column: "createdAt");

            migrationBuilder.CreateIndex(
                name: "IDX_50bd7164c5b78f1f4a42c4d21f",
                table: "poll_vote",
                columns: new[] { "userId", "noteId", "choice" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IDX_66d2bd2ee31d14bcc23069a89f",
                table: "poll_vote",
                column: "userId");

            migrationBuilder.CreateIndex(
                name: "IDX_aecfbd5ef60374918e63ee95fa",
                table: "poll_vote",
                column: "noteId");

            migrationBuilder.CreateIndex(
                name: "IDX_83f0862e9bae44af52ced7099e",
                table: "promo_note",
                column: "userId");

            migrationBuilder.CreateIndex(
                name: "IDX_2882b8a1a07c7d281a98b6db16",
                table: "promo_read",
                columns: new[] { "userId", "noteId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IDX_9657d55550c3d37bfafaf7d4b0",
                table: "promo_read",
                column: "userId");

            migrationBuilder.CreateIndex(
                name: "IX_promo_read_noteId",
                table: "promo_read",
                column: "noteId");

            migrationBuilder.CreateIndex(
                name: "IDX_0ff69e8dfa9fe31bb4a4660f59",
                table: "registration_ticket",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IDX_0a72bdfcdb97c0eca11fe7ecad",
                table: "registry_item",
                column: "domain");

            migrationBuilder.CreateIndex(
                name: "IDX_22baca135bb8a3ea1a83d13df3",
                table: "registry_item",
                column: "scope");

            migrationBuilder.CreateIndex(
                name: "IDX_fb9d21ba0abb83223263df6bcb",
                table: "registry_item",
                column: "userId");

            migrationBuilder.CreateIndex(
                name: "IDX_0d9a1738f2cf7f3b1c3334dfab",
                table: "relay",
                column: "inbox",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IDX_0d801c609cec4e9eb4b6b4490c",
                table: "renote_muting",
                columns: new[] { "muterId", "muteeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IDX_7aa72a5fe76019bfe8e5e0e8b7",
                table: "renote_muting",
                column: "muterId");

            migrationBuilder.CreateIndex(
                name: "IDX_7eac97594bcac5ffcf2068089b",
                table: "renote_muting",
                column: "muteeId");

            migrationBuilder.CreateIndex(
                name: "IDX_d1259a2c2b7bb413ff449e8711",
                table: "renote_muting",
                column: "createdAt");

            migrationBuilder.CreateIndex(
                name: "IDX_232f8e85d7633bd6ddfad42169",
                table: "session",
                column: "token");

            migrationBuilder.CreateIndex(
                name: "IX_session_userId",
                table: "session",
                column: "userId");

            migrationBuilder.CreateIndex(
                name: "IDX_2c308dbdc50d94dc625670055f",
                table: "signin",
                column: "userId");

            migrationBuilder.CreateIndex(
                name: "IDX_97754ca6f2baff9b4abb7f853d",
                table: "sw_subscription",
                column: "userId");

            migrationBuilder.CreateIndex(
                name: "IDX_3252a5df8d5bbd16b281f7799e",
                table: "user",
                column: "host");

            migrationBuilder.CreateIndex(
                name: "IDX_5deb01ae162d1d70b80d064c27",
                table: "user",
                columns: new[] { "usernameLower", "host" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IDX_80ca6e6ef65fb9ef34ea8c90f4",
                table: "user",
                column: "updatedAt");

            migrationBuilder.CreateIndex(
                name: "IDX_a27b942a0d6dcff90e3ee9b5e8",
                table: "user",
                column: "usernameLower");

            migrationBuilder.CreateIndex(
                name: "IDX_a854e557b1b14814750c7c7b0c",
                table: "user",
                column: "token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IDX_be623adaa4c566baf5d29ce0c8",
                table: "user",
                column: "uri");

            migrationBuilder.CreateIndex(
                name: "IDX_c8cc87bd0f2f4487d17c651fbf",
                table: "user",
                column: "lastActiveDate");

            migrationBuilder.CreateIndex(
                name: "IDX_d5a1b83c7cab66f167e6888188",
                table: "user",
                column: "isExplorable");

            migrationBuilder.CreateIndex(
                name: "IDX_e11e649824a45d8ed01d597fd9",
                table: "user",
                column: "createdAt");

            migrationBuilder.CreateIndex(
                name: "IDX_fa99d777623947a5b05f394cae",
                table: "user",
                column: "tags");

            migrationBuilder.CreateIndex(
                name: "REL_58f5c71eaab331645112cf8cfa",
                table: "user",
                column: "avatarId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "REL_afc64b53f8db3707ceb34eb28e",
                table: "user",
                column: "bannerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_a854e557b1b14814750c7c7b0c9",
                table: "user",
                column: "token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IDX_20e30aa35180e317e133d75316",
                table: "user_group",
                column: "createdAt");

            migrationBuilder.CreateIndex(
                name: "IDX_3d6b372788ab01be58853003c9",
                table: "user_group",
                column: "userId");

            migrationBuilder.CreateIndex(
                name: "IDX_5cc8c468090e129857e9fecce5",
                table: "user_group_invitation",
                column: "userGroupId");

            migrationBuilder.CreateIndex(
                name: "IDX_bfbc6305547539369fe73eb144",
                table: "user_group_invitation",
                column: "userId");

            migrationBuilder.CreateIndex(
                name: "IDX_e9793f65f504e5a31fbaedbf2f",
                table: "user_group_invitation",
                columns: new[] { "userId", "userGroupId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IDX_1039988afa3bf991185b277fe0",
                table: "user_group_invite",
                column: "userId");

            migrationBuilder.CreateIndex(
                name: "IDX_78787741f9010886796f2320a4",
                table: "user_group_invite",
                columns: new[] { "userId", "userGroupId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IDX_e10924607d058004304611a436",
                table: "user_group_invite",
                column: "userGroupId");

            migrationBuilder.CreateIndex(
                name: "IDX_67dc758bc0566985d1b3d39986",
                table: "user_group_joining",
                column: "userGroupId");

            migrationBuilder.CreateIndex(
                name: "IDX_d9ecaed8c6dc43f3592c229282",
                table: "user_group_joining",
                columns: new[] { "userId", "userGroupId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IDX_f3a1b4bd0c7cabba958a0c0b23",
                table: "user_group_joining",
                column: "userId");

            migrationBuilder.CreateIndex(
                name: "IDX_b7fcefbdd1c18dce86687531f9",
                table: "user_list",
                column: "userId");

            migrationBuilder.CreateIndex(
                name: "IDX_605472305f26818cc93d1baaa7",
                table: "user_list_joining",
                column: "userListId");

            migrationBuilder.CreateIndex(
                name: "IDX_90f7da835e4c10aca6853621e1",
                table: "user_list_joining",
                columns: new[] { "userId", "userListId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IDX_d844bfc6f3f523a05189076efa",
                table: "user_list_joining",
                column: "userId");

            migrationBuilder.CreateIndex(
                name: "IDX_410cd649884b501c02d6e72738",
                table: "user_note_pining",
                columns: new[] { "userId", "noteId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IDX_bfbc6f79ba4007b4ce5097f08d",
                table: "user_note_pining",
                column: "userId");

            migrationBuilder.CreateIndex(
                name: "IX_user_note_pining_noteId",
                table: "user_note_pining",
                column: "noteId");

            migrationBuilder.CreateIndex(
                name: "IDX_4e5c4c99175638ec0761714ab0",
                table: "user_pending",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IDX_3befe6f999c86aff06eb0257b4",
                table: "user_profile",
                column: "enableWordMute");

            migrationBuilder.CreateIndex(
                name: "IDX_dce530b98e454793dac5ec2f5a",
                table: "user_profile",
                column: "userHost");

            migrationBuilder.CreateIndex(
                name: "UQ_6dc44f1ceb65b1e72bacef2ca27",
                table: "user_profile",
                column: "pinnedPageId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IDX_171e64971c780ebd23fae140bb",
                table: "user_publickey",
                column: "keyId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IDX_0d7718e562dcedd0aa5cf2c9f7",
                table: "user_security_key",
                column: "publicKey");

            migrationBuilder.CreateIndex(
                name: "IDX_ff9ca3b5f3ee3d0681367a9b44",
                table: "user_security_key",
                column: "userId");

            migrationBuilder.CreateIndex(
                name: "IDX_5a056076f76b2efe08216ba655",
                table: "webhook",
                column: "active");

            migrationBuilder.CreateIndex(
                name: "IDX_8063a0586ed1dfbe86e982d961",
                table: "webhook",
                column: "on");

            migrationBuilder.CreateIndex(
                name: "IDX_f272c8c8805969e6a6449c77b3",
                table: "webhook",
                column: "userId");

            migrationBuilder.AddForeignKey(
                name: "FK_04cc96756f89d0b7f9473e8cdf3",
                table: "abuse_user_report",
                column: "reporterId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_08b883dd5fdd6f9c4c1572b36de",
                table: "abuse_user_report",
                column: "assigneeId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_a9021cc2e1feb5f72d3db6e9f5f",
                table: "abuse_user_report",
                column: "targetUserId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_9949557d0e1b2c19e5344c171e9",
                table: "access_token",
                column: "userId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_a3ff16c90cc87a82a0b5959e560",
                table: "access_token",
                column: "appId",
                principalTable: "app",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_8288151386172b8109f7239ab28",
                table: "announcement_read",
                column: "userId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_6446c571a0e8d0f05f01c789096",
                table: "antenna",
                column: "userId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_709d7d32053d0dd7620f678eeb9",
                table: "antenna",
                column: "userListId",
                principalTable: "user_list",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ccbf5a8c0be4511133dcc50ddeb",
                table: "antenna",
                column: "userGroupJoiningId",
                principalTable: "user_group_joining",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_3f5b0899ef90527a3462d7c2cb3",
                table: "app",
                column: "userId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_f1a461a618fa1755692d0e0d592",
                table: "attestation_challenge",
                column: "userId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_c072b729d71697f959bde66ade0",
                table: "auth_session",
                column: "userId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_0627125f1a8a42c9a1929edb552",
                table: "blocking",
                column: "blockerId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_2cd4a2743a99671308f5417759e",
                table: "blocking",
                column: "blockeeId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_823bae55bd81b3be6e05cff4383",
                table: "channel",
                column: "userId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_999da2bcc7efadbfe0e92d3bc19",
                table: "channel",
                column: "bannerId",
                principalTable: "drive_file",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_6d8084ec9496e7334a4602707e1",
                table: "channel_following",
                column: "followerId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_10b19ef67d297ea9de325cd4502",
                table: "channel_note_pining",
                column: "noteId",
                principalTable: "note",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_2b5ec6c574d6802c94c80313fb2",
                table: "clip",
                column: "userId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_a012eaf5c87c65da1deb5fdbfa3",
                table: "clip_note",
                column: "noteId",
                principalTable: "note",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_860fa6f6c7df5bb887249fba22e",
                table: "drive_file",
                column: "userId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_bb90d1956dafc4068c28aa7560a",
                table: "drive_file",
                column: "folderId",
                principalTable: "drive_folder",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_860fa6f6c7df5bb887249fba22e",
                table: "drive_file");

            migrationBuilder.DropForeignKey(
                name: "FK_f4fc06e49c0171c85f1c48060d2",
                table: "drive_folder");

            migrationBuilder.DropTable(
                name: "abuse_user_report");

            migrationBuilder.DropTable(
                name: "announcement_read");

            migrationBuilder.DropTable(
                name: "antenna");

            migrationBuilder.DropTable(
                name: "attestation_challenge");

            migrationBuilder.DropTable(
                name: "auth_session");

            migrationBuilder.DropTable(
                name: "blocking");

            migrationBuilder.DropTable(
                name: "channel_following");

            migrationBuilder.DropTable(
                name: "channel_note_pining");

            migrationBuilder.DropTable(
                name: "clip_note");

            migrationBuilder.DropTable(
                name: "emoji");

            migrationBuilder.DropTable(
                name: "following");

            migrationBuilder.DropTable(
                name: "gallery_like");

            migrationBuilder.DropTable(
                name: "hashtag");

            migrationBuilder.DropTable(
                name: "html_note_cache_entry");

            migrationBuilder.DropTable(
                name: "html_user_cache_entry");

            migrationBuilder.DropTable(
                name: "instance");

            migrationBuilder.DropTable(
                name: "messaging_message");

            migrationBuilder.DropTable(
                name: "meta");

            migrationBuilder.DropTable(
                name: "moderation_log");

            migrationBuilder.DropTable(
                name: "muting");

            migrationBuilder.DropTable(
                name: "note_edit");

            migrationBuilder.DropTable(
                name: "note_favorite");

            migrationBuilder.DropTable(
                name: "note_reaction");

            migrationBuilder.DropTable(
                name: "note_thread_muting");

            migrationBuilder.DropTable(
                name: "note_unread");

            migrationBuilder.DropTable(
                name: "note_watching");

            migrationBuilder.DropTable(
                name: "notification");

            migrationBuilder.DropTable(
                name: "oauth_token");

            migrationBuilder.DropTable(
                name: "page_like");

            migrationBuilder.DropTable(
                name: "password_reset_request");

            migrationBuilder.DropTable(
                name: "poll");

            migrationBuilder.DropTable(
                name: "poll_vote");

            migrationBuilder.DropTable(
                name: "promo_note");

            migrationBuilder.DropTable(
                name: "promo_read");

            migrationBuilder.DropTable(
                name: "registration_ticket");

            migrationBuilder.DropTable(
                name: "registry_item");

            migrationBuilder.DropTable(
                name: "relay");

            migrationBuilder.DropTable(
                name: "renote_muting");

            migrationBuilder.DropTable(
                name: "session");

            migrationBuilder.DropTable(
                name: "signin");

            migrationBuilder.DropTable(
                name: "sw_subscription");

            migrationBuilder.DropTable(
                name: "used_username");

            migrationBuilder.DropTable(
                name: "user_group_invite");

            migrationBuilder.DropTable(
                name: "user_keypair");

            migrationBuilder.DropTable(
                name: "user_list_joining");

            migrationBuilder.DropTable(
                name: "user_note_pining");

            migrationBuilder.DropTable(
                name: "user_pending");

            migrationBuilder.DropTable(
                name: "user_profile");

            migrationBuilder.DropTable(
                name: "user_publickey");

            migrationBuilder.DropTable(
                name: "user_security_key");

            migrationBuilder.DropTable(
                name: "webhook");

            migrationBuilder.DropTable(
                name: "announcement");

            migrationBuilder.DropTable(
                name: "user_group_joining");

            migrationBuilder.DropTable(
                name: "clip");

            migrationBuilder.DropTable(
                name: "gallery_post");

            migrationBuilder.DropTable(
                name: "user_group_invitation");

            migrationBuilder.DropTable(
                name: "follow_request");

            migrationBuilder.DropTable(
                name: "access_token");

            migrationBuilder.DropTable(
                name: "oauth_app");

            migrationBuilder.DropTable(
                name: "user_list");

            migrationBuilder.DropTable(
                name: "note");

            migrationBuilder.DropTable(
                name: "page");

            migrationBuilder.DropTable(
                name: "user_group");

            migrationBuilder.DropTable(
                name: "app");

            migrationBuilder.DropTable(
                name: "channel");

            migrationBuilder.DropTable(
                name: "user");

            migrationBuilder.DropTable(
                name: "drive_file");

            migrationBuilder.DropTable(
                name: "drive_folder");
        }
    }
}
