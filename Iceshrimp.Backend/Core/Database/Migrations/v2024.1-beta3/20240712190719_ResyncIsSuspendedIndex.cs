using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace Iceshrimp.Backend.Core.Database.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(DatabaseContext))]
    [Migration("20240712190719_ResyncIsSuspendedIndex")]
    public partial class ResyncIsSuspendedIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
	        migrationBuilder.Sql("""
	                             ALTER INDEX IF EXISTS "IDX_8977c6037a7bc2cb0c84b6d4db" RENAME TO "IX_user_isSuspended";
	                             CREATE INDEX IF NOT EXISTS "IX_user_isSuspended" ON "user" ("isSuspended");
	                             """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
	        migrationBuilder.Sql("""
	                             ALTER INDEX "IX_user_isSuspended" RENAME TO "IDX_8977c6037a7bc2cb0c84b6d4db";
	                             """);
        }
    }
}
