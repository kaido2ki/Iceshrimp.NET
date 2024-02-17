using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Iceshrimp.Backend.Core.Database.Migrations
{
    /// <inheritdoc />
    public partial class RenameNoteBookmarksTable2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_note_favorite_note_noteId",
                table: "note_favorite");

            migrationBuilder.DropForeignKey(
                name: "FK_note_favorite_user_userId",
                table: "note_favorite");

            migrationBuilder.DropPrimaryKey(
                name: "PK_note_favorite",
                table: "note_favorite");

            migrationBuilder.RenameTable(
                name: "note_favorite",
                newName: "note_bookmark");

            migrationBuilder.RenameIndex(
                name: "IX_note_favorite_userId_noteId",
                table: "note_bookmark",
                newName: "IX_note_bookmark_userId_noteId");

            migrationBuilder.RenameIndex(
                name: "IX_note_favorite_userId",
                table: "note_bookmark",
                newName: "IX_note_bookmark_userId");

            migrationBuilder.RenameIndex(
                name: "IX_note_favorite_noteId",
                table: "note_bookmark",
                newName: "IX_note_bookmark_noteId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_note_bookmark",
                table: "note_bookmark",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_note_bookmark_note_noteId",
                table: "note_bookmark",
                column: "noteId",
                principalTable: "note",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_note_bookmark_user_userId",
                table: "note_bookmark",
                column: "userId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_note_bookmark_note_noteId",
                table: "note_bookmark");

            migrationBuilder.DropForeignKey(
                name: "FK_note_bookmark_user_userId",
                table: "note_bookmark");

            migrationBuilder.DropPrimaryKey(
                name: "PK_note_bookmark",
                table: "note_bookmark");

            migrationBuilder.RenameTable(
                name: "note_bookmark",
                newName: "note_favorite");

            migrationBuilder.RenameIndex(
                name: "IX_note_bookmark_userId_noteId",
                table: "note_favorite",
                newName: "IX_note_favorite_userId_noteId");

            migrationBuilder.RenameIndex(
                name: "IX_note_bookmark_userId",
                table: "note_favorite",
                newName: "IX_note_favorite_userId");

            migrationBuilder.RenameIndex(
                name: "IX_note_bookmark_noteId",
                table: "note_favorite",
                newName: "IX_note_favorite_noteId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_note_favorite",
                table: "note_favorite",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_note_favorite_note_noteId",
                table: "note_favorite",
                column: "noteId",
                principalTable: "note",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_note_favorite_user_userId",
                table: "note_favorite",
                column: "userId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
