using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace Iceshrimp.Backend.Core.Database.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(DatabaseContext))]
    [Migration("20240930222445_CanonicalizeUserHostCapitalization")]
    public partial class CanonicalizeUserHostCapitalization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
	        migrationBuilder.Sql("""
	                             UPDATE "abuse_user_report" SET "targetUserHost" = LOWER("targetUserHost") WHERE "targetUserHost" <> LOWER("targetUserHost");
	                             UPDATE "abuse_user_report" SET "reporterHost" = LOWER("reporterHost") WHERE "reporterHost" <> LOWER("reporterHost");
	                             UPDATE "allowed_instance" SET "host" = LOWER("host") WHERE "host" <> LOWER("host");
	                             UPDATE "bite" SET "userHost" = LOWER("userHost") WHERE "userHost" <> LOWER("userHost");
	                             UPDATE "blocked_instance" SET "host" = LOWER("host") WHERE "host" <> LOWER("host");
	                             UPDATE "drive_file" SET "userHost" = LOWER("userHost") WHERE "userHost" <> LOWER("userHost");
	                             UPDATE "emoji" SET "host" = LOWER("host") WHERE "host" <> LOWER("host");
	                             UPDATE "follow_request" SET "followerHost" = LOWER("followerHost") WHERE "followerHost" <> LOWER("followerHost");
	                             UPDATE "follow_request" SET "followeeHost" = LOWER("followeeHost") WHERE "followeeHost" <> LOWER("followeeHost");
	                             UPDATE "following" SET "followerHost" = LOWER("followerHost") WHERE "followerHost" <> LOWER("followerHost");
	                             UPDATE "following" SET "followeeHost" = LOWER("followeeHost") WHERE "followeeHost" <> LOWER("followeeHost");
	                             UPDATE "instance" SET "host" = LOWER("host") WHERE "host" <> LOWER("host");
	                             UPDATE "note" SET "userHost" = LOWER("userHost") WHERE "userHost" <> LOWER("userHost");
	                             UPDATE "note" SET "replyUserHost" = LOWER("replyUserHost") WHERE "replyUserHost" <> LOWER("replyUserHost");
	                             UPDATE "note" SET "renoteUserHost" = LOWER("renoteUserHost") WHERE "renoteUserHost" <> LOWER("renoteUserHost");
	                             UPDATE "poll" SET "userHost" = LOWER("userHost") WHERE "userHost" <> LOWER("userHost");
	                             UPDATE "user" SET "host" = LOWER("host") WHERE "host" <> LOWER("host");
	                             UPDATE "user_profile" SET "userHost" = LOWER("userHost"), "mentionsResolved" = FALSE, "mentions" = '[]'::jsonb WHERE "userHost" <> LOWER("userHost");
	                             """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
