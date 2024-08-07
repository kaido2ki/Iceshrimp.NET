using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace Iceshrimp.Backend.Core.Database.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(DatabaseContext))]
    [Migration("20240808010539_AddDriveFileThumbnailTypeColumn")]
    public partial class AddDriveFileThumbnailTypeColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "thumbnailType",
                table: "drive_file",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.Sql("""
                                 UPDATE "drive_file" SET "thumbnailType" = 'image/webp' WHERE "thumbnailAccessKey" IS NOT NULL AND "thumbnailUrl" IS NOT NULL;
                                 """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "thumbnailType",
                table: "drive_file");
        }
    }
}
