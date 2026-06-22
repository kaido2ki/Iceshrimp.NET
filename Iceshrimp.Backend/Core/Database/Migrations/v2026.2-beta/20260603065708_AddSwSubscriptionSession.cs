using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace Iceshrimp.Backend.Core.Database.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(DatabaseContext))]
    [Migration("20260603065708_AddSwSubscriptionSession")]
    public partial class AddSwSubscriptionSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Clear old sw_subscriptios from migrated Iceshrimp-js databases
            migrationBuilder.Sql("DELETE FROM \"sw_subscription\";");

            migrationBuilder.AddColumn<string>(
                name: "sessionId",
                table: "sw_subscription",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_sw_subscription_sessionId",
                table: "sw_subscription",
                column: "sessionId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_sw_subscription_session_sessionId",
                table: "sw_subscription",
                column: "sessionId",
                principalTable: "session",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_sw_subscription_session_sessionId",
                table: "sw_subscription");

            migrationBuilder.DropIndex(
                name: "IX_sw_subscription_sessionId",
                table: "sw_subscription");

            migrationBuilder.DropColumn(
                name: "sessionId",
                table: "sw_subscription");
        }
    }
}
