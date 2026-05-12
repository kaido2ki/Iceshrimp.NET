using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace Iceshrimp.Backend.Core.Database.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(DatabaseContext))]
    [Migration("20260512204439_FixRegistrationInviteCreatedByDeleteConstraint")]
    public partial class FixRegistrationInviteCreatedByDeleteConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_registration_invite_user_createdById",
                table: "registration_invite");

            migrationBuilder.AddForeignKey(
                name: "FK_registration_invite_user_createdById",
                table: "registration_invite",
                column: "createdById",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_registration_invite_user_createdById",
                table: "registration_invite");

            migrationBuilder.AddForeignKey(
                name: "FK_registration_invite_user_createdById",
                table: "registration_invite",
                column: "createdById",
                principalTable: "user",
                principalColumn: "id");
        }
    }
}
