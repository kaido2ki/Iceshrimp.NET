using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Iceshrimp.Backend.Core.Database.Migrations
{
    /// <inheritdoc />
    public partial class RenameRegistrationInviteTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_registration_ticket",
                table: "registration_ticket");

            migrationBuilder.RenameTable(
                name: "registration_ticket",
                newName: "registration_invite");

            migrationBuilder.RenameIndex(
                name: "IX_registration_ticket_code",
                table: "registration_invite",
                newName: "IX_registration_invite_code");

            migrationBuilder.AddPrimaryKey(
                name: "PK_registration_invite",
                table: "registration_invite",
                column: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_registration_invite",
                table: "registration_invite");

            migrationBuilder.RenameTable(
                name: "registration_invite",
                newName: "registration_ticket");

            migrationBuilder.RenameIndex(
                name: "IX_registration_invite_code",
                table: "registration_ticket",
                newName: "IX_registration_ticket_code");

            migrationBuilder.AddPrimaryKey(
                name: "PK_registration_ticket",
                table: "registration_ticket",
                column: "id");
        }
    }
}
