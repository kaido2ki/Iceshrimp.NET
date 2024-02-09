using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Iceshrimp.Backend.Core.Database.Migrations
{
    /// <inheritdoc />
    public partial class MoveDriveHashAlgoToSha256 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_drive_file_md5",
                table: "drive_file");

            migrationBuilder.DropColumn(
                name: "md5",
                table: "drive_file");

            migrationBuilder.AddColumn<string>(
                name: "sha256",
                table: "drive_file",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true,
                comment: "The SHA256 hash of the DriveFile.");

            migrationBuilder.CreateIndex(
                name: "IX_drive_file_sha256",
                table: "drive_file",
                column: "sha256");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_drive_file_sha256",
                table: "drive_file");

            migrationBuilder.DropColumn(
                name: "sha256",
                table: "drive_file");

            migrationBuilder.AddColumn<string>(
                name: "md5",
                table: "drive_file",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "",
                comment: "The MD5 hash of the DriveFile.");

            migrationBuilder.CreateIndex(
                name: "IX_drive_file_md5",
                table: "drive_file",
                column: "md5");
        }
    }
}
