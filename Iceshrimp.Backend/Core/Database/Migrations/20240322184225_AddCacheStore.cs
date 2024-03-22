using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Iceshrimp.Backend.Core.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddCacheStore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cache_store",
                columns: table => new
                {
                    key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    value = table.Column<string>(type: "text", nullable: true),
                    expiry = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ttl = table.Column<TimeSpan>(type: "interval", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cache_store", x => x.key);
                });

            migrationBuilder.CreateIndex(
                name: "IX_cache_store_expiry",
                table: "cache_store",
                column: "expiry");

            migrationBuilder.CreateIndex(
                name: "IX_cache_store_key_value_expiry",
                table: "cache_store",
                columns: new[] { "key", "value", "expiry" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cache_store");
        }
    }
}
