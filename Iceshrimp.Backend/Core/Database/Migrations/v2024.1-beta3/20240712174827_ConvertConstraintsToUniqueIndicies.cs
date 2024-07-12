using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace Iceshrimp.Backend.Core.Database.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(DatabaseContext))]
    [Migration("20240712174827_ConvertConstraintsToUniqueIndicies")]
    public partial class ConvertConstraintsToUniqueIndicies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
	        migrationBuilder.Sql("""
	                             ALTER TABLE "user" DROP CONSTRAINT IF EXISTS "IX_user_avatarId";
	                             ALTER TABLE "user" DROP CONSTRAINT IF EXISTS "IX_user_bannerId";
	                             ALTER TABLE "user_profile" DROP CONSTRAINT IF EXISTS "IX_user_profile_pinnedPageId";
	                             CREATE UNIQUE INDEX IF NOT EXISTS "IX_user_avatarId" ON "user" ("avatarId");
	                             CREATE UNIQUE INDEX IF NOT EXISTS "IX_user_bannerId" ON "user" ("bannerId");
	                             CREATE UNIQUE INDEX IF NOT EXISTS "IX_user_profile_pinnedPageId" ON "user_profile" ("pinnedPageId");
	                             """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
	        migrationBuilder.Sql("""
	                             DROP INDEX "IX_user_avatarId";
	                             DROP INDEX "IX_user_bannerId";
	                             DROP INDEX "IX_user_profile_pinnedPageId";
	                             ALTER TABLE "user" ADD CONSTRAINT "IX_user_avatarId" UNIQUE ("avatarId");
	                             ALTER TABLE "user" ADD CONSTRAINT "IX_user_bannerId" UNIQUE ("bannerId");
	                             ALTER TABLE "user_profile" ADD CONSTRAINT "IX_user_profile_pinnedPageId" UNIQUE ("pinnedPageId");
	                             """);
        }
    }
}
