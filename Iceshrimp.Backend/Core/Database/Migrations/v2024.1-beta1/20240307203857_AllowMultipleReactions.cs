using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace Iceshrimp.Backend.Core.Database.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(DatabaseContext))]
    [Migration("20240307203857_AllowMultipleReactions")]
    public partial class AllowMultipleReactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_note_reaction_userId_noteId",
                table: "note_reaction");

            migrationBuilder.CreateIndex(
                name: "IX_note_reaction_userId_noteId_reaction",
                table: "note_reaction",
                columns: new[] { "userId", "noteId", "reaction" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_note_reaction_userId_noteId_reaction",
                table: "note_reaction");

            migrationBuilder.CreateIndex(
                name: "IX_note_reaction_userId_noteId",
                table: "note_reaction",
                columns: new[] { "userId", "noteId" },
                unique: true);
        }
    }
}
