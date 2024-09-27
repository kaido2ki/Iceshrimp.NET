using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace Iceshrimp.Backend.Core.Database.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(DatabaseContext))]
    [Migration("20240927214613_AddRelayAndSystemUserColumns")]
    public partial class AddRelayAndSystemUserColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "isRelayActor",
                table: "user",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "isSystem",
                table: "user",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql("""
                                 UPDATE "user" SET "isSystem" = TRUE WHERE "host" IS NULL AND ("usernameLower" = 'instance.actor' OR "usernameLower" = 'relay.actor');
                                 """);
            
            migrationBuilder.Sql("""
                                 UPDATE "user" SET "isRelayActor" = TRUE WHERE "inbox" IN (SELECT "inbox" FROM "relay");
                                 """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "isRelayActor",
                table: "user");

            migrationBuilder.DropColumn(
                name: "isSystem",
                table: "user");
        }
    }
}
