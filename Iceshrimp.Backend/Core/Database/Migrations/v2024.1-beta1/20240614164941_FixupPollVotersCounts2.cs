using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace Iceshrimp.Backend.Core.Database.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(DatabaseContext))]
    [Migration("20240614164941_FixupPollVotersCounts2")]
    public partial class FixupPollVotersCounts2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
	        migrationBuilder.Sql("""UPDATE "poll" SET "votersCount" = (SELECT COUNT(*) FROM (SELECT DISTINCT "userId" FROM "poll_vote" WHERE "noteId" = "poll"."noteId") AS sq) WHERE "userHost" IS NULL AND "votersCount" IS NOT NULL;""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
