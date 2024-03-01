using System;
using Iceshrimp.Backend.Controllers.Mastodon.Schemas.Entities;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Iceshrimp.Backend.Core.Database.Migrations
{
    /// <inheritdoc />
    public partial class RemoveHtmlCache : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "html_note_cache_entry");

            migrationBuilder.DropTable(
                name: "html_user_cache_entry");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "html_note_cache_entry",
                columns: table => new
                {
                    noteId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    content = table.Column<string>(type: "text", nullable: true),
                    updatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_html_note_cache_entry", x => x.noteId);
                    table.ForeignKey(
                        name: "FK_html_note_cache_entry_note_noteId",
                        column: x => x.noteId,
                        principalTable: "note",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "html_user_cache_entry",
                columns: table => new
                {
                    userId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    bio = table.Column<string>(type: "text", nullable: true),
                    fields = table.Column<Field[]>(type: "jsonb", nullable: false, defaultValueSql: "'[]'::jsonb"),
                    updatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_html_user_cache_entry", x => x.userId);
                    table.ForeignKey(
                        name: "FK_html_user_cache_entry_user_userId",
                        column: x => x.userId,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });
        }
    }
}
