using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace Iceshrimp.Backend.Core.Database.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(DatabaseContext))]
    [Migration("20250513105104_MastodonLinkedWebSession")]
    public partial class MastodonLinkedWebSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "mastodonTokenId",
                table: "session",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_session_mastodonTokenId",
                table: "session",
                column: "mastodonTokenId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_session_oauth_token_mastodonTokenId",
                table: "session",
                column: "mastodonTokenId",
                principalTable: "oauth_token",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_session_oauth_token_mastodonTokenId",
                table: "session");

            migrationBuilder.DropIndex(
                name: "IX_session_mastodonTokenId",
                table: "session");

            migrationBuilder.DropColumn(
                name: "mastodonTokenId",
                table: "session");
        }
    }
}
