using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Iceshrimp.Backend.Core.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddNoteMastoReplyUserIdColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "mastoReplyUserId",
                table: "note",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "mastoReplyUserId",
                table: "note");
        }
    }
}
