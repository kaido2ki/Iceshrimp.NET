using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace Iceshrimp.Backend.Core.Database.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(DatabaseContext))]
    [Migration("20240620180339_FixupPollVotersCounts3")]
    public partial class FixupPollVotersCounts3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
	        migrationBuilder.Sql("""
	                             UPDATE "poll" SET "votersCount" = GREATEST("votersCount", (SELECT MAX("total") FROM UNNEST("votes") AS "total")::integer, (SELECT COUNT(*) FROM (SELECT DISTINCT "userId" FROM "poll_vote" WHERE "noteId" = "poll"."noteId") AS sq)::integer) WHERE "votersCount" IS NOT NULL AND "multiple" = TRUE;
	                             UPDATE "poll" SET "votersCount" = GREATEST("votersCount", (SELECT SUM("total") FROM UNNEST("votes") AS "total")::integer, (SELECT COUNT(*) FROM (SELECT DISTINCT "userId" FROM "poll_vote" WHERE "noteId" = "poll"."noteId") AS sq)::integer) WHERE "votersCount" IS NOT NULL AND "multiple" = FALSE;
	                             """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
