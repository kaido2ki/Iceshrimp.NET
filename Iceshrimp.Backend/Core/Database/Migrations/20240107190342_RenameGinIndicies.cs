using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Iceshrimp.Backend.Core.Database.Migrations
{
    /// <inheritdoc />
    public partial class RenameGinIndicies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "note_text_fts_idx",
                table: "note",
                newName: "GIN_TRGM_note_text");

            migrationBuilder.RenameIndex(
                name: "IDX_NOTE_VISIBLE_USER_IDS",
                table: "note",
                newName: "GIN_note_visibleUserIds");

            migrationBuilder.RenameIndex(
                name: "IDX_NOTE_TAGS",
                table: "note",
                newName: "GIN_note_tags");

            migrationBuilder.RenameIndex(
                name: "IDX_NOTE_MENTIONS",
                table: "note",
                newName: "GIN_note_mentions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "GIN_note_visibleUserIds",
                table: "note",
                newName: "IDX_NOTE_VISIBLE_USER_IDS");

            migrationBuilder.RenameIndex(
                name: "GIN_note_tags",
                table: "note",
                newName: "IDX_NOTE_TAGS");

            migrationBuilder.RenameIndex(
                name: "GIN_note_mentions",
                table: "note",
                newName: "IDX_NOTE_MENTIONS");

            migrationBuilder.RenameIndex(
                name: "GIN_TRGM_note_text",
                table: "note",
                newName: "note_text_fts_idx");
        }
    }
}
