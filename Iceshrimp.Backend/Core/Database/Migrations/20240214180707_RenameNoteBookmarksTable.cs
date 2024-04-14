using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Iceshrimp.Backend.Core.Database.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(DatabaseContext))]
    [Migration("20240214180707_RenameNoteBookmarksTable")]
    public partial class RenameNoteBookmarksTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "createdAt",
                table: "note_favorite",
                type: "timestamp with time zone",
                nullable: false,
                comment: "The created date of the NoteBookmark.",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldComment: "The created date of the NoteFavorite.");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "createdAt",
                table: "note_favorite",
                type: "timestamp with time zone",
                nullable: false,
                comment: "The created date of the NoteFavorite.",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldComment: "The created date of the NoteBookmark.");
        }
    }
}
