using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace Iceshrimp.Backend.Core.Database.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(DatabaseContext))]
    [Migration("20240118165641_AddUserRoleIndicies")]
    public partial class AddUserRoleIndicies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_user_isAdmin",
                table: "user",
                column: "isAdmin");

            migrationBuilder.CreateIndex(
                name: "IX_user_isModerator",
                table: "user",
                column: "isModerator");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_user_isAdmin",
                table: "user");

            migrationBuilder.DropIndex(
                name: "IX_user_isModerator",
                table: "user");
        }
    }
}
