using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace Iceshrimp.Backend.Core.Database.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(DatabaseContext))]
    [Migration("20250109095535_RemoteUserAvatarBannerUrlColumns")]
    public partial class RemoteUserAvatarBannerUrlColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "avatarUrl",
                table: "user");

            migrationBuilder.DropColumn(
                name: "bannerUrl",
                table: "user");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "avatarUrl",
                table: "user",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true,
                comment: "The URL of the avatar DriveFile");

            migrationBuilder.AddColumn<string>(
                name: "bannerUrl",
                table: "user",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true,
                comment: "The URL of the banner DriveFile");

            migrationBuilder.Sql("""
                                 UPDATE "user" SET "avatarUrl" = (SELECT COALESCE("webpublicUrl", "url") FROM "drive_file" WHERE "id" = "user"."avatarId");
                                 UPDATE "user" SET "bannerUrl" = (SELECT COALESCE("webpublicUrl", "url") FROM "drive_file" WHERE "id" = "user"."bannerId");
                                 """);
        }
    }
}
