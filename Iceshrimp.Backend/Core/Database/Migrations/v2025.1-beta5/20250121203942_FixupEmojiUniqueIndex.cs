using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace Iceshrimp.Backend.Core.Database.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(DatabaseContext))]
    [Migration("20250121203942_FixupEmojiUniqueIndex")]
    public partial class FixupEmojiUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_emoji_name_host",
                table: "emoji");

            // Clean up duplicates, preserving the newest one
            migrationBuilder.Sql("""
                                 DELETE FROM "emoji" e USING "emoji" e2 WHERE e."host" IS NULL AND e."name" = e2."name" AND e2."host" IS NULL AND e."id" < e2."id";
                                 """);

            migrationBuilder.CreateIndex(
                name: "IX_emoji_name_host",
                table: "emoji",
                columns: new[] { "name", "host" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_emoji_name_host",
                table: "emoji");

            migrationBuilder.CreateIndex(
                name: "IX_emoji_name_host",
                table: "emoji",
                columns: new[] { "name", "host" },
                unique: true);
        }
    }
}
