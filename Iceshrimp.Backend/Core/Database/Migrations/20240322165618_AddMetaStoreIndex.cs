using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace Iceshrimp.Backend.Core.Database.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(DatabaseContext))]
    [Migration("20240322165618_AddMetaStoreIndex")]
    public partial class AddMetaStoreIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_meta_store_key",
                table: "meta_store");

            migrationBuilder.CreateIndex(
                name: "IX_meta_store_key_value",
                table: "meta_store",
                columns: new[] { "key", "value" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_meta_store_key_value",
                table: "meta_store");

            migrationBuilder.CreateIndex(
                name: "IX_meta_store_key",
                table: "meta_store",
                column: "key");
        }
    }
}
