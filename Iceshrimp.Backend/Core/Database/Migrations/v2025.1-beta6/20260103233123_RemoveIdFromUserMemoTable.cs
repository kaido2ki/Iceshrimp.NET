using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace Iceshrimp.Backend.Core.Database.Migrations.v2025._1beta6
{
    /// <inheritdoc />
    [DbContext(typeof(DatabaseContext))]
    [Migration("20260103233123_RemoveIdFromUserMemoTable")]
    public partial class RemoveIdFromUserMemoTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_user_memo",
                table: "user_memo");

            migrationBuilder.DropColumn(
                name: "id",
                table: "user_memo");

            migrationBuilder.AddPrimaryKey(
                name: "PK_user_memo",
                table: "user_memo",
                columns: new[] { "by_user_id", "target_user_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_user_memo",
                table: "user_memo");

            migrationBuilder.AddColumn<string>(
                name: "id",
                table: "user_memo",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_user_memo",
                table: "user_memo",
                column: "id");
        }
    }
}
