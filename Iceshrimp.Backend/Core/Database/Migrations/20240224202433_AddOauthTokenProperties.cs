using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Iceshrimp.Backend.Core.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddOauthTokenProperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "autoDetectQuotes",
                table: "oauth_token",
                type: "boolean",
                nullable: false,
                defaultValue: true,
                comment: "Whether the backend should automatically detect quote posts coming from this client");

            migrationBuilder.AddColumn<bool>(
                name: "supportsHtmlFormatting",
                table: "oauth_token",
                type: "boolean",
                nullable: false,
                defaultValue: true,
                comment: "Whether the client supports HTML inline formatting (bold, italic, strikethrough, ...)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "autoDetectQuotes",
                table: "oauth_token");

            migrationBuilder.DropColumn(
                name: "supportsHtmlFormatting",
                table: "oauth_token");
        }
    }
}
