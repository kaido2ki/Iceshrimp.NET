using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Iceshrimp.Backend.Core.Database.Migrations
{
    /// <inheritdoc />
    public partial class TokenPleroma : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "isPleroma",
                table: "oauth_token",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                comment: "Whether Pleroma or Akkoma specific behavior should be enabled for this client");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "isPleroma",
                table: "oauth_token");
        }
    }
}
