using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Iceshrimp.Backend.Core.Database.Tables;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Iceshrimp.Backend.Core.Database.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(DatabaseContext))]
    [Migration("20240709231619_RemoveExtraneousUserProfileColumns")]
    public partial class RemoveExtraneousUserProfileColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_user_profile_enableWordMute",
                table: "user_profile");

            migrationBuilder.DropColumn(
                name: "carefulBot",
                table: "user_profile");

            migrationBuilder.DropColumn(
                name: "clientData",
                table: "user_profile");

            migrationBuilder.DropColumn(
                name: "emailNotificationTypes",
                table: "user_profile");

            migrationBuilder.DropColumn(
                name: "emailVerifyCode",
                table: "user_profile");

            migrationBuilder.DropColumn(
                name: "enableWordMute",
                table: "user_profile");

            migrationBuilder.DropColumn(
                name: "injectFeaturedNote",
                table: "user_profile");

            migrationBuilder.DropColumn(
                name: "integrations",
                table: "user_profile");

            migrationBuilder.DropColumn(
                name: "mutedInstances",
                table: "user_profile");

            migrationBuilder.DropColumn(
                name: "mutedWords",
                table: "user_profile");

            migrationBuilder.DropColumn(
                name: "mutingNotificationTypes",
                table: "user_profile");

            migrationBuilder.DropColumn(
                name: "noCrawle",
                table: "user_profile");

            migrationBuilder.DropColumn(
                name: "preventAiLearning",
                table: "user_profile");

            migrationBuilder.DropColumn(
                name: "publicReactions",
                table: "user_profile");

            migrationBuilder.DropColumn(
                name: "receiveAnnouncementEmail",
                table: "user_profile");

            migrationBuilder.DropColumn(
                name: "room",
                table: "user_profile");

            migrationBuilder.DropColumn(
                name: "securityKeysAvailable",
                table: "user_profile");

            migrationBuilder.DropColumn(
                name: "usePasswordLessLogin",
                table: "user_profile");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "carefulBot",
                table: "user_profile",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "clientData",
                table: "user_profile",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'{}'::jsonb",
                comment: "The client-specific data of the User.");

            migrationBuilder.AddColumn<List<string>>(
                name: "emailNotificationTypes",
                table: "user_profile",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'[\"follow\", \"receiveFollowRequest\", \"groupInvited\"]'::jsonb");

            migrationBuilder.AddColumn<string>(
                name: "emailVerifyCode",
                table: "user_profile",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "enableWordMute",
                table: "user_profile",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "injectFeaturedNote",
                table: "user_profile",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "integrations",
                table: "user_profile",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'{}'::jsonb");

            migrationBuilder.AddColumn<List<string>>(
                name: "mutedInstances",
                table: "user_profile",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'[]'::jsonb",
                comment: "List of instances muted by the user.");

            migrationBuilder.AddColumn<string>(
                name: "mutedWords",
                table: "user_profile",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'[]'::jsonb");

            migrationBuilder.AddColumn<List<Notification.NotificationType>>(
                name: "mutingNotificationTypes",
                table: "user_profile",
                type: "notification_type_enum[]",
                nullable: false,
                defaultValueSql: "'{}'::public.notification_type_enum[]");

            migrationBuilder.AddColumn<bool>(
                name: "noCrawle",
                table: "user_profile",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                comment: "Whether reject index by crawler.");

            migrationBuilder.AddColumn<bool>(
                name: "preventAiLearning",
                table: "user_profile",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "publicReactions",
                table: "user_profile",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "receiveAnnouncementEmail",
                table: "user_profile",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "room",
                table: "user_profile",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'{}'::jsonb",
                comment: "The room data of the User.");

            migrationBuilder.AddColumn<bool>(
                name: "securityKeysAvailable",
                table: "user_profile",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "usePasswordLessLogin",
                table: "user_profile",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_user_profile_enableWordMute",
                table: "user_profile",
                column: "enableWordMute");
        }
    }
}
