using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Iceshrimp.Backend.Core.Database.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(DatabaseContext))]
    [Migration("20240304201225_RemoveHashtagStatisticsColumns")]
    public partial class RemoveHashtagStatisticsColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_hashtag_attachedLocalUsersCount",
                table: "hashtag");

            migrationBuilder.DropIndex(
                name: "IX_hashtag_attachedRemoteUsersCount",
                table: "hashtag");

            migrationBuilder.DropIndex(
                name: "IX_hashtag_attachedUsersCount",
                table: "hashtag");

            migrationBuilder.DropIndex(
                name: "IX_hashtag_mentionedLocalUsersCount",
                table: "hashtag");

            migrationBuilder.DropIndex(
                name: "IX_hashtag_mentionedRemoteUsersCount",
                table: "hashtag");

            migrationBuilder.DropIndex(
                name: "IX_hashtag_mentionedUsersCount",
                table: "hashtag");

            migrationBuilder.DropColumn(
                name: "attachedLocalUserIds",
                table: "hashtag");

            migrationBuilder.DropColumn(
                name: "attachedLocalUsersCount",
                table: "hashtag");

            migrationBuilder.DropColumn(
                name: "attachedRemoteUserIds",
                table: "hashtag");

            migrationBuilder.DropColumn(
                name: "attachedRemoteUsersCount",
                table: "hashtag");

            migrationBuilder.DropColumn(
                name: "attachedUserIds",
                table: "hashtag");

            migrationBuilder.DropColumn(
                name: "attachedUsersCount",
                table: "hashtag");

            migrationBuilder.DropColumn(
                name: "mentionedLocalUserIds",
                table: "hashtag");

            migrationBuilder.DropColumn(
                name: "mentionedLocalUsersCount",
                table: "hashtag");

            migrationBuilder.DropColumn(
                name: "mentionedRemoteUserIds",
                table: "hashtag");

            migrationBuilder.DropColumn(
                name: "mentionedRemoteUsersCount",
                table: "hashtag");

            migrationBuilder.DropColumn(
                name: "mentionedUserIds",
                table: "hashtag");

            migrationBuilder.DropColumn(
                name: "mentionedUsersCount",
                table: "hashtag");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<List<string>>(
                name: "attachedLocalUserIds",
                table: "hashtag",
                type: "character varying(32)[]",
                nullable: false);

            migrationBuilder.AddColumn<int>(
                name: "attachedLocalUsersCount",
                table: "hashtag",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<List<string>>(
                name: "attachedRemoteUserIds",
                table: "hashtag",
                type: "character varying(32)[]",
                nullable: false);

            migrationBuilder.AddColumn<int>(
                name: "attachedRemoteUsersCount",
                table: "hashtag",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<List<string>>(
                name: "attachedUserIds",
                table: "hashtag",
                type: "character varying(32)[]",
                nullable: false);

            migrationBuilder.AddColumn<int>(
                name: "attachedUsersCount",
                table: "hashtag",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<List<string>>(
                name: "mentionedLocalUserIds",
                table: "hashtag",
                type: "character varying(32)[]",
                nullable: false);

            migrationBuilder.AddColumn<int>(
                name: "mentionedLocalUsersCount",
                table: "hashtag",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<List<string>>(
                name: "mentionedRemoteUserIds",
                table: "hashtag",
                type: "character varying(32)[]",
                nullable: false);

            migrationBuilder.AddColumn<int>(
                name: "mentionedRemoteUsersCount",
                table: "hashtag",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<List<string>>(
                name: "mentionedUserIds",
                table: "hashtag",
                type: "character varying(32)[]",
                nullable: false);

            migrationBuilder.AddColumn<int>(
                name: "mentionedUsersCount",
                table: "hashtag",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_hashtag_attachedLocalUsersCount",
                table: "hashtag",
                column: "attachedLocalUsersCount");

            migrationBuilder.CreateIndex(
                name: "IX_hashtag_attachedRemoteUsersCount",
                table: "hashtag",
                column: "attachedRemoteUsersCount");

            migrationBuilder.CreateIndex(
                name: "IX_hashtag_attachedUsersCount",
                table: "hashtag",
                column: "attachedUsersCount");

            migrationBuilder.CreateIndex(
                name: "IX_hashtag_mentionedLocalUsersCount",
                table: "hashtag",
                column: "mentionedLocalUsersCount");

            migrationBuilder.CreateIndex(
                name: "IX_hashtag_mentionedRemoteUsersCount",
                table: "hashtag",
                column: "mentionedRemoteUsersCount");

            migrationBuilder.CreateIndex(
                name: "IX_hashtag_mentionedUsersCount",
                table: "hashtag",
                column: "mentionedUsersCount");
        }
    }
}
