using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace Iceshrimp.Backend.Core.Database.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(DatabaseContext))]
    [Migration("20241106151513_AddNoteThreadContext")]
    public partial class AddNoteThreadContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "isResolvable",
                table: "note_thread",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "uri",
                table: "note_thread",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "userId",
                table: "note_thread",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_note_thread_userId",
                table: "note_thread",
                column: "userId");

            migrationBuilder.AddForeignKey(
                name: "FK_note_thread_user_userId",
                table: "note_thread",
                column: "userId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_note_thread_user_userId",
                table: "note_thread");

            migrationBuilder.DropIndex(
                name: "IX_note_thread_userId",
                table: "note_thread");

            migrationBuilder.DropColumn(
                name: "isResolvable",
                table: "note_thread");

            migrationBuilder.DropColumn(
                name: "uri",
                table: "note_thread");

            migrationBuilder.DropColumn(
                name: "userId",
                table: "note_thread");
        }
    }
}
