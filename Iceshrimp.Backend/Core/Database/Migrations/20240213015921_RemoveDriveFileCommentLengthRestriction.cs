using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace Iceshrimp.Backend.Core.Database.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(DatabaseContext))]
    [Migration("20240213015921_RemoveDriveFileCommentLengthRestriction")]
    public partial class RemoveDriveFileCommentLengthRestriction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "comment",
                table: "drive_file",
                type: "text",
                nullable: true,
                comment: "The comment of the DriveFile.",
                oldClrType: typeof(string),
                oldType: "character varying(8192)",
                oldMaxLength: 8192,
                oldNullable: true,
                oldComment: "The comment of the DriveFile.");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "comment",
                table: "drive_file",
                type: "character varying(8192)",
                maxLength: 8192,
                nullable: true,
                comment: "The comment of the DriveFile.",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true,
                oldComment: "The comment of the DriveFile.");
        }
    }
}
