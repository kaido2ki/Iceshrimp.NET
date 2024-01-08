using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Iceshrimp.Backend.Core.Database.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUnneededTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_group_invite");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "user_group_invite",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    userGroupId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    userId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    createdAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_group_invite", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_group_invite_user_group_userGroupId",
                        column: x => x.userGroupId,
                        principalTable: "user_group",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_group_invite_user_userId",
                        column: x => x.userId,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_user_group_invite_userGroupId",
                table: "user_group_invite",
                column: "userGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_user_group_invite_userId",
                table: "user_group_invite",
                column: "userId");

            migrationBuilder.CreateIndex(
                name: "IX_user_group_invite_userId_userGroupId",
                table: "user_group_invite",
                columns: new[] { "userId", "userGroupId" },
                unique: true);
        }
    }
}
