using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace Iceshrimp.Backend.Core.Database.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(DatabaseContext))]
    [Migration("20240312232809_AddNoteCwIndex")]
    public partial class AddNoteCwIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
	        Console.WriteLine("Indexing note content warnings, please hang tight!");
	        Console.WriteLine("This may take a long time (15-30 minutes), especially if your database is unusually large or you're running low end hardware.");
	        
            migrationBuilder.CreateIndex(
                name: "GIN_TRGM_note_cw",
                table: "note",
                column: "cw")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "GIN_TRGM_note_cw",
                table: "note");
        }
    }
}
