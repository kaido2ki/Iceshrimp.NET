using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Iceshrimp.Backend.Core.Database.Migrations.v2025._1beta6
{
    /// <inheritdoc />
    [DbContext(typeof(DatabaseContext))]
    [Migration("20251019071520_AddScheduling")]
    public partial class AddScheduling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "published",
                table: "note",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "scheduledAt",
                table: "note",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "published",
                table: "note");

            migrationBuilder.DropColumn(
                name: "scheduledAt",
                table: "note");
        }
    }
}
