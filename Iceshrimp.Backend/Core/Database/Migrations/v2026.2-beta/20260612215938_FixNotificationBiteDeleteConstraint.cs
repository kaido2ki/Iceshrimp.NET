using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace Iceshrimp.Backend.Core.Database.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(DatabaseContext))]
    [Migration("20260612215938_FixNotificationBiteDeleteConstraint")]
    public partial class FixNotificationBiteDeleteConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_notification_bite_biteId",
                table: "notification");

            migrationBuilder.AddForeignKey(
                name: "FK_notification_bite_biteId",
                table: "notification",
                column: "biteId",
                principalTable: "bite",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_notification_bite_biteId",
                table: "notification");

            migrationBuilder.AddForeignKey(
                name: "FK_notification_bite_biteId",
                table: "notification",
                column: "biteId",
                principalTable: "bite",
                principalColumn: "id");
        }
    }
}
