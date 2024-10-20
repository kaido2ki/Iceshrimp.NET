using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace Iceshrimp.Backend.Core.Database.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(DatabaseContext))]
    [Migration("20241020194126_IncreaseFollowRequestIdMaxLength")]
    public partial class IncreaseFollowRequestIdMaxLength : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "requestId",
                table: "follow_request",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true,
                comment: "id of Follow Activity.",
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128,
                oldNullable: true,
                oldComment: "id of Follow Activity.");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "requestId",
                table: "follow_request",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true,
                comment: "id of Follow Activity.",
                oldClrType: typeof(string),
                oldType: "character varying(512)",
                oldMaxLength: 512,
                oldNullable: true,
                oldComment: "id of Follow Activity.");
        }
    }
}
