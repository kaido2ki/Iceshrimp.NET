using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Iceshrimp.Backend.Core.Database.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(DatabaseContext))]
    [Migration("20260623000958_AddNoteTranslationTable")]
    public partial class AddNoteTranslationTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "note_translation",
                columns: table => new
                {
                    noteId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    targetLanguage = table.Column<string>(type: "text", nullable: false),
                    noteEditId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    text = table.Column<string>(type: "text", nullable: true),
                    cw = table.Column<string>(type: "text", nullable: true),
                    originalLanguage = table.Column<string>(type: "text", nullable: false),
                    pollChoices = table.Column<List<string>>(type: "character varying(256)[]", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_note_translation", x => new { x.noteId, x.targetLanguage });
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "note_translation");
        }
    }
}
