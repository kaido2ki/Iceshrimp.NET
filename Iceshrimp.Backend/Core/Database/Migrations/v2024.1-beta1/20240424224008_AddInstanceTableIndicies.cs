using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace Iceshrimp.Backend.Core.Database.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(DatabaseContext))]
    [Migration("20240424224008_AddInstanceTableIndicies")]
    public partial class AddInstanceTableIndicies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_instance_incomingFollows",
                table: "instance",
                column: "incomingFollows");

            migrationBuilder.CreateIndex(
                name: "IX_instance_outgoingFollows",
                table: "instance",
                column: "outgoingFollows");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_instance_incomingFollows",
                table: "instance");

            migrationBuilder.DropIndex(
                name: "IX_instance_outgoingFollows",
                table: "instance");
        }
    }
}
