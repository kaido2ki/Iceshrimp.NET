using Iceshrimp.Backend.Core.Database.Tables;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Iceshrimp.Backend.Core.Database.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(DatabaseContext))]
    [Migration("20240428210826_AddDefaultRenoteVisibilityUserSetting")]
    public partial class AddDefaultRenoteVisibilityUserSetting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Note.NoteVisibility>(
                name: "defaultRenoteVisibility",
                table: "user_settings",
                type: "note_visibility_enum",
                nullable: false,
                defaultValue: Note.NoteVisibility.Public);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "defaultRenoteVisibility",
                table: "user_settings");
        }
    }
}
