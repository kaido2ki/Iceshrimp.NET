using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Iceshrimp.Backend.Core.Database.Tables;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Iceshrimp.Backend.Core.Database.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(DatabaseContext))]
    [Migration("20250410120227_AnnouncementsMfm")]
    public partial class AnnouncementsMfm : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<List<string>>(
                name: "emojis",
                table: "announcement",
                type: "character varying(128)[]",
                nullable: false,
                defaultValueSql: "'{}'::character varying[]");

            migrationBuilder.AddColumn<List<string>>(
                name: "mentions",
                table: "announcement",
                type: "character varying(32)[]",
                nullable: false,
                defaultValueSql: "'{}'::character varying[]");

            migrationBuilder.AddColumn<List<string>>(
                name: "tags",
                table: "announcement",
                type: "character varying(128)[]",
                nullable: false,
                defaultValueSql: "'{}'::character varying[]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "emojis",
                table: "announcement");

            migrationBuilder.DropColumn(
                name: "mentions",
                table: "announcement");

            migrationBuilder.DropColumn(
                name: "tags",
                table: "announcement");
        }
    }
}
