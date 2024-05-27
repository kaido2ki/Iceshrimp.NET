using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace Iceshrimp.Backend.Core.Database.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(DatabaseContext))]
    [Migration("20240429190728_AddNoteRenoteReplyUriColumns")]
    public partial class AddNoteRenoteReplyUriColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "renoteUri",
                table: "note",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true,
                comment: "The URI of the renote target, if it couldn't be resolved at time of ingestion.");

            migrationBuilder.AddColumn<string>(
                name: "replyUri",
                table: "note",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true,
                comment: "The URI of the reply target, if it couldn't be resolved at time of ingestion.");

            migrationBuilder.CreateIndex(
                name: "IX_note_renoteUri",
                table: "note",
                column: "renoteUri");

            migrationBuilder.CreateIndex(
                name: "IX_note_replyUri",
                table: "note",
                column: "replyUri");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_note_renoteUri",
                table: "note");

            migrationBuilder.DropIndex(
                name: "IX_note_replyUri",
                table: "note");

            migrationBuilder.DropColumn(
                name: "renoteUri",
                table: "note");

            migrationBuilder.DropColumn(
                name: "replyUri",
                table: "note");
        }
    }
}
