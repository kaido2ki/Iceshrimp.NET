using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Iceshrimp.Backend.Core.Database.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(DatabaseContext))]
    [Migration("20240214175919_AddNoteLikeTable")]
    public partial class AddNoteLikeTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "note_like",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    createdAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    userId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    noteId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_note_like", x => x.id);
                    table.ForeignKey(
                        name: "FK_note_like_note_noteId",
                        column: x => x.noteId,
                        principalTable: "note",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_note_like_user_userId",
                        column: x => x.userId,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_note_like_noteId",
                table: "note_like",
                column: "noteId");

            migrationBuilder.CreateIndex(
                name: "IX_note_like_userId",
                table: "note_like",
                column: "userId");

            migrationBuilder.CreateIndex(
                name: "IX_note_like_userId_noteId",
                table: "note_like",
                columns: new[] { "userId", "noteId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "note_like");
        }
    }
}
