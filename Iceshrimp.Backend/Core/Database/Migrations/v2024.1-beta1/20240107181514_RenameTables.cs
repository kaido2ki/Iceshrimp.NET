using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace Iceshrimp.Backend.Core.Database.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(DatabaseContext))]
    [Migration("20240107181514_RenameTables")]
    public partial class RenameTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "user_note_pining",
                newName: "user_note_pin");

            migrationBuilder.RenameTable(
                name: "user_list_joining",
                newName: "user_list_member");

            migrationBuilder.RenameTable(
                name: "user_group_joining",
                newName: "user_group_member");

            migrationBuilder.RenameTable(
                name: "channel_note_pining",
                newName: "channel_note_pin");

            migrationBuilder.RenameIndex(
                name: "IX_user_note_pining_noteId",
                table: "user_note_pin",
                newName: "IX_user_note_pin_noteId");

            migrationBuilder.RenameIndex(
                name: "IX_channel_note_pining_noteId",
                table: "channel_note_pin",
                newName: "IX_channel_note_pin_noteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "user_note_pin",
                newName: "user_note_pining");

            migrationBuilder.RenameTable(
                name: "user_list_member",
                newName: "user_list_joining");

            migrationBuilder.RenameTable(
                name: "user_group_member",
                newName: "user_group_joining");

            migrationBuilder.RenameTable(
                name: "channel_note_pin",
                newName: "channel_note_pining");

            migrationBuilder.RenameIndex(
                name: "IX_user_note_pin_noteId",
                table: "user_note_pining",
                newName: "IX_user_note_pining_noteId");

            migrationBuilder.RenameIndex(
                name: "IX_channel_note_pin_noteId",
                table: "channel_note_pining",
                newName: "IX_channel_note_pining_noteId");
        }
    }
}
