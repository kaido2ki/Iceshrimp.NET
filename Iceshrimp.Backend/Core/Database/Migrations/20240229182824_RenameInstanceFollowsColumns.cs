using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace Iceshrimp.Backend.Core.Database.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(DatabaseContext))]
    [Migration("20240229182824_RenameInstanceFollowsColumns")]
    public partial class RenameInstanceFollowsColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "followingCount",
                table: "instance",
                newName: "incomingFollows");

            migrationBuilder.RenameColumn(
                name: "followersCount",
                table: "instance",
                newName: "outgoingFollows");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "outgoingFollows",
                table: "instance",
                newName: "followersCount");

            migrationBuilder.RenameColumn(
                name: "incomingFollows",
                table: "instance",
                newName: "followingCount");
        }
    }
}
