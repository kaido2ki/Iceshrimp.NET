using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Iceshrimp.Backend.Core.Database.Migrations
{
    /// <inheritdoc />
    public partial class FixupDeleteConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_notification_access_token_appAccessTokenId",
                table: "notification");

            migrationBuilder.DropTable(
                name: "access_token");

            migrationBuilder.DropTable(
                name: "auth_session");

            migrationBuilder.DropTable(
                name: "signin");

            migrationBuilder.DropTable(
                name: "app");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "app",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    userId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true, comment: "The owner ID."),
                    callbackUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true, comment: "The callbackUrl of the App."),
                    createdAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "The created date of the App."),
                    description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false, comment: "The description of the App."),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false, comment: "The name of the App."),
                    permission = table.Column<List<string>>(type: "character varying(64)[]", nullable: false, comment: "The permission of the App."),
                    secret = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, comment: "The secret key of the App.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_app", x => x.id);
                    table.ForeignKey(
                        name: "FK_app_user_userId",
                        column: x => x.userId,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "signin",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    userId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    createdAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "The created date of the Signin."),
                    headers = table.Column<Dictionary<string, string>>(type: "jsonb", nullable: false),
                    ip = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    success = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_signin", x => x.id);
                    table.ForeignKey(
                        name: "FK_signin_user_userId",
                        column: x => x.userId,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "access_token",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    appId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    userId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    createdAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "The created date of the AccessToken."),
                    description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    fetched = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    iconUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    lastUsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    permission = table.Column<List<string>>(type: "character varying(64)[]", nullable: false, defaultValueSql: "'{}'::character varying[]"),
                    session = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    token = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_access_token", x => x.id);
                    table.ForeignKey(
                        name: "FK_access_token_app_appId",
                        column: x => x.appId,
                        principalTable: "app",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_access_token_user_userId",
                        column: x => x.userId,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "auth_session",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    appId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    userId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    createdAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "The created date of the AuthSession."),
                    token = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_auth_session", x => x.id);
                    table.ForeignKey(
                        name: "FK_auth_session_app_appId",
                        column: x => x.appId,
                        principalTable: "app",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_auth_session_user_userId",
                        column: x => x.userId,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_access_token_appId",
                table: "access_token",
                column: "appId");

            migrationBuilder.CreateIndex(
                name: "IX_access_token_hash",
                table: "access_token",
                column: "hash");

            migrationBuilder.CreateIndex(
                name: "IX_access_token_session",
                table: "access_token",
                column: "session");

            migrationBuilder.CreateIndex(
                name: "IX_access_token_token",
                table: "access_token",
                column: "token");

            migrationBuilder.CreateIndex(
                name: "IX_access_token_userId",
                table: "access_token",
                column: "userId");

            migrationBuilder.CreateIndex(
                name: "IX_app_createdAt",
                table: "app",
                column: "createdAt");

            migrationBuilder.CreateIndex(
                name: "IX_app_secret",
                table: "app",
                column: "secret");

            migrationBuilder.CreateIndex(
                name: "IX_app_userId",
                table: "app",
                column: "userId");

            migrationBuilder.CreateIndex(
                name: "IX_auth_session_appId",
                table: "auth_session",
                column: "appId");

            migrationBuilder.CreateIndex(
                name: "IX_auth_session_token",
                table: "auth_session",
                column: "token");

            migrationBuilder.CreateIndex(
                name: "IX_auth_session_userId",
                table: "auth_session",
                column: "userId");

            migrationBuilder.CreateIndex(
                name: "IX_signin_userId",
                table: "signin",
                column: "userId");

            migrationBuilder.AddForeignKey(
                name: "FK_notification_access_token_appAccessTokenId",
                table: "notification",
                column: "appAccessTokenId",
                principalTable: "access_token",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
