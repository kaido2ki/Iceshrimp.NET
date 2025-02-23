using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Iceshrimp.Backend.Core.Database.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(DatabaseContext))]
    [Migration("20250223092435_AddUserLastNoteAt")]
    public partial class AddUserLastNoteAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "lastNoteAt",
                table: "user",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.Sql("""UPDATE "user" SET "lastNoteAt" = (SELECT note."createdAt" FROM "note" WHERE "note"."userId" = "user"."id" ORDER BY "note"."createdAt" DESC LIMIT 1);""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "lastNoteAt",
                table: "user");
        }
    }
}
