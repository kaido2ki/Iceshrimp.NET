using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace Iceshrimp.Backend.Core.Database.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(DatabaseContext))]
    [Migration("20251117122639_FixNotificationReportDeleteBehavior")]
    public partial class FixNotificationReportDeleteBehavior : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_notification_report_reportId",
                table: "notification");

            migrationBuilder.AddForeignKey(
                name: "FK_notification_report_reportId",
                table: "notification",
                column: "reportId",
                principalTable: "report",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_notification_report_reportId",
                table: "notification");

            migrationBuilder.AddForeignKey(
                name: "FK_notification_report_reportId",
                table: "notification",
                column: "reportId",
                principalTable: "report",
                principalColumn: "id");
        }
    }
}
