using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace Iceshrimp.Backend.Core.Database.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(DatabaseContext))]
    [Migration("20240831215254_TokenPleroma")]
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
