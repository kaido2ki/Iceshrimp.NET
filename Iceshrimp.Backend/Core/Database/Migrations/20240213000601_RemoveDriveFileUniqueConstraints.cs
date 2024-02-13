using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Iceshrimp.Backend.Core.Database.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDriveFileUniqueConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_drive_file_accessKey",
                table: "drive_file");

            migrationBuilder.DropIndex(
                name: "IX_drive_file_thumbnailAccessKey",
                table: "drive_file");

            migrationBuilder.DropIndex(
                name: "IX_drive_file_webpublicAccessKey",
                table: "drive_file");

            migrationBuilder.CreateIndex(
                name: "IX_drive_file_accessKey",
                table: "drive_file",
                column: "accessKey");

            migrationBuilder.CreateIndex(
                name: "IX_drive_file_thumbnailAccessKey",
                table: "drive_file",
                column: "thumbnailAccessKey");

            migrationBuilder.CreateIndex(
                name: "IX_drive_file_webpublicAccessKey",
                table: "drive_file",
                column: "webpublicAccessKey");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_drive_file_accessKey",
                table: "drive_file");

            migrationBuilder.DropIndex(
                name: "IX_drive_file_thumbnailAccessKey",
                table: "drive_file");

            migrationBuilder.DropIndex(
                name: "IX_drive_file_webpublicAccessKey",
                table: "drive_file");

            migrationBuilder.CreateIndex(
                name: "IX_drive_file_accessKey",
                table: "drive_file",
                column: "accessKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_drive_file_thumbnailAccessKey",
                table: "drive_file",
                column: "thumbnailAccessKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_drive_file_webpublicAccessKey",
                table: "drive_file",
                column: "webpublicAccessKey",
                unique: true);
        }
    }
}
