using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace Iceshrimp.Backend.Core.Database.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(DatabaseContext))]
    [Migration("20250323112015_RenameBubbleInstanceTable")]
    public partial class RenameBubbleInstanceTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_recommended_instance",
                table: "recommended_instance");

            migrationBuilder.RenameTable(
                name: "recommended_instance",
                newName: "bubble_instance");

            migrationBuilder.AddPrimaryKey(
                name: "PK_bubble_instance",
                table: "bubble_instance",
                column: "host");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_bubble_instance",
                table: "bubble_instance");

            migrationBuilder.RenameTable(
                name: "bubble_instance",
                newName: "recommended_instance");

            migrationBuilder.AddPrimaryKey(
                name: "PK_recommended_instance",
                table: "recommended_instance",
                column: "host");
        }
    }
}
