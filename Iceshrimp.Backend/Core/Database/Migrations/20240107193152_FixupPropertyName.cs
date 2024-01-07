using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Iceshrimp.Backend.Core.Database.Migrations
{
    /// <inheritdoc />
    public partial class FixupPropertyName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_antenna_user_group_member_UserGroupMemberId",
                table: "antenna");

            migrationBuilder.RenameColumn(
                name: "UserGroupMemberId",
                table: "antenna",
                newName: "userGroupMemberId");

            migrationBuilder.RenameIndex(
                name: "IX_antenna_UserGroupMemberId",
                table: "antenna",
                newName: "IX_antenna_userGroupMemberId");

            migrationBuilder.AddForeignKey(
                name: "FK_antenna_user_group_member_userGroupMemberId",
                table: "antenna",
                column: "userGroupMemberId",
                principalTable: "user_group_member",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_antenna_user_group_member_userGroupMemberId",
                table: "antenna");

            migrationBuilder.RenameColumn(
                name: "userGroupMemberId",
                table: "antenna",
                newName: "UserGroupMemberId");

            migrationBuilder.RenameIndex(
                name: "IX_antenna_userGroupMemberId",
                table: "antenna",
                newName: "IX_antenna_UserGroupMemberId");

            migrationBuilder.AddForeignKey(
                name: "FK_antenna_user_group_member_UserGroupMemberId",
                table: "antenna",
                column: "UserGroupMemberId",
                principalTable: "user_group_member",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
