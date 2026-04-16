using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace Iceshrimp.Backend.Core.Database.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(DatabaseContext))]
    [Migration("20250409145744_AddRegistrationInviteCreatedByColumn")]
    public partial class AddRegistrationInviteCreatedByColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "createdById",
                table: "registration_invite",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_registration_invite_createdById",
                table: "registration_invite",
                column: "createdById");

            migrationBuilder.AddForeignKey(
                name: "FK_registration_invite_user_createdById",
                table: "registration_invite",
                column: "createdById",
                principalTable: "user",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_registration_invite_user_createdById",
                table: "registration_invite");

            migrationBuilder.DropIndex(
                name: "IX_registration_invite_createdById",
                table: "registration_invite");

            migrationBuilder.DropColumn(
                name: "createdById",
                table: "registration_invite");
        }
    }
}
