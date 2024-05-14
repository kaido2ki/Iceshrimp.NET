using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace Iceshrimp.Backend.Core.Database.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(DatabaseContext))]
    [Migration("20240514181009_FixupDriveFileMetadata")]
    public partial class FixupDriveFileMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
	        migrationBuilder.Sql("UPDATE "drive_file" SET "thumbnailAccessKey" = NULL WHERE "thumbnailUrl" IS NULL AND "thumbnailAccessKey" IS NOT NULL;");
	        migrationBuilder.Sql("UPDATE "drive_file" SET "webpublicAccessKey" = NULL WHERE "webpublicUrl" IS NULL AND "webpublicAccessKey" IS NOT NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
