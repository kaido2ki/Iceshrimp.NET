using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace Iceshrimp.Backend.Core.Database.Migrations.v2025._1beta6
{
    /// <inheritdoc />
    [DbContext(typeof(DatabaseContext))]
    [Migration("20251021213532_AddHideRepliesNotFollowingSetting")]
    public partial class AddHideRepliesNotFollowingSetting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "hideRepliesNotFollowing",
                table: "user_settings",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "hideRepliesNotFollowing",
                table: "user_settings");
        }
    }
}
