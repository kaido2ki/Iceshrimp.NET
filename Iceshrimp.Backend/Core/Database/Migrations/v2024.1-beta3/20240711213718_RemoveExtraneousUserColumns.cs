using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace Iceshrimp.Backend.Core.Database.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(DatabaseContext))]
    [Migration("20240711213718_RemoveExtraneousUserColumns")]
    public partial class RemoveExtraneousUserColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
	        migrationBuilder.Sql("""
	                             ALTER TABLE "user" DROP CONSTRAINT IF EXISTS "IX_user_token";
	                             DROP INDEX IF EXISTS "IX_user_token";
	                             """);

            migrationBuilder.DropColumn(
                name: "hideOnlineStatus",
                table: "user");

            migrationBuilder.DropColumn(
                name: "token",
                table: "user");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "hideOnlineStatus",
                table: "user",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "token",
                table: "user",
                type: "character(16)",
                fixedLength: true,
                maxLength: 16,
                nullable: true,
                comment: "The native access token of the User. It will be null if the origin of the user is local.");

            migrationBuilder.CreateIndex(
                name: "IX_user_token",
                table: "user",
                column: "token",
                unique: true);
        }
    }
}
