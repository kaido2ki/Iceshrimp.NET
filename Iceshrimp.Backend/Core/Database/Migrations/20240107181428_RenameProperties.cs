using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Iceshrimp.Backend.Core.Database.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(DatabaseContext))]
    [Migration("20240107181428_RenameProperties")]
    public partial class RenameProperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "userGroupJoiningId",
                table: "antenna",
                newName: "UserGroupMemberId");

            migrationBuilder.RenameIndex(
                name: "IX_antenna_userGroupJoiningId",
                table: "antenna",
                newName: "IX_antenna_UserGroupMemberId");

            migrationBuilder.AlterColumn<DateTime>(
                name: "createdAt",
                table: "user_note_pining",
                type: "timestamp with time zone",
                nullable: false,
                comment: "The created date of the UserNotePins.",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldComment: "The created date of the UserNotePinings.");

            migrationBuilder.AlterColumn<DateTime>(
                name: "createdAt",
                table: "user_list_joining",
                type: "timestamp with time zone",
                nullable: false,
                comment: "The created date of the UserListMember.",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldComment: "The created date of the UserListJoining.");

            migrationBuilder.AlterColumn<DateTime>(
                name: "createdAt",
                table: "user_group_joining",
                type: "timestamp with time zone",
                nullable: false,
                comment: "The created date of the UserGroupMember.",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldComment: "The created date of the UserGroupJoining.");

            migrationBuilder.AlterColumn<DateTime>(
                name: "createdAt",
                table: "channel_note_pining",
                type: "timestamp with time zone",
                nullable: false,
                comment: "The created date of the ChannelNotePin.",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldComment: "The created date of the ChannelNotePining.");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UserGroupMemberId",
                table: "antenna",
                newName: "userGroupJoiningId");

            migrationBuilder.RenameIndex(
                name: "IX_antenna_UserGroupMemberId",
                table: "antenna",
                newName: "IX_antenna_userGroupJoiningId");

            migrationBuilder.AlterColumn<DateTime>(
                name: "createdAt",
                table: "user_note_pining",
                type: "timestamp with time zone",
                nullable: false,
                comment: "The created date of the UserNotePinings.",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldComment: "The created date of the UserNotePins.");

            migrationBuilder.AlterColumn<DateTime>(
                name: "createdAt",
                table: "user_list_joining",
                type: "timestamp with time zone",
                nullable: false,
                comment: "The created date of the UserListJoining.",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldComment: "The created date of the UserListMember.");

            migrationBuilder.AlterColumn<DateTime>(
                name: "createdAt",
                table: "user_group_joining",
                type: "timestamp with time zone",
                nullable: false,
                comment: "The created date of the UserGroupJoining.",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldComment: "The created date of the UserGroupMember.");

            migrationBuilder.AlterColumn<DateTime>(
                name: "createdAt",
                table: "channel_note_pining",
                type: "timestamp with time zone",
                nullable: false,
                comment: "The created date of the ChannelNotePining.",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldComment: "The created date of the ChannelNotePin.");
        }
    }
}
