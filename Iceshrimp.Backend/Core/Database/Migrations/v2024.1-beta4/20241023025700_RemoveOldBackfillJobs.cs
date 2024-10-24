using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace Iceshrimp.Backend.Core.Database.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(DatabaseContext))]
    [Migration("20241023025700_RemoveOldBackfillJobs")]
    public partial class RemoveOldBackfillJobs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // the data format for backfill jobs have backwards-incompatibly changed since the introduction of the feature
            // we can drop all old jobs without causing too much trouble as they will be re-scheduled on demand
            migrationBuilder.Sql("""DELETE FROM "jobs" WHERE "queue" = 'backfill';""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // nothing to do!
        }
    }
}
