using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace Iceshrimp.Backend.Core.Database.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(DatabaseContext))]
    [Migration("20241203204110_AddInlineHtmlFeatureFlag")]
    public partial class AddInlineHtmlFeatureFlag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "supportsInlineMedia",
                table: "oauth_token",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // automatically enable inline media for pleroma clients, as pleroma itself does support it
            migrationBuilder.Sql("""UPDATE "oauth_token" SET "supportsInlineMedia"=TRUE WHERE "isPleroma"=TRUE;""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "supportsInlineMedia",
                table: "oauth_token");
        }
    }
}
