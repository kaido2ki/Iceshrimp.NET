using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace Iceshrimp.Backend.Core.Database.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(DatabaseContext))]
    [Migration("20240527231353_AddNoteCombinedAltTextField")]
    public partial class AddNoteCombinedAltTextField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "combinedAltText",
                table: "note",
                type: "text",
                nullable: true);

            Console.WriteLine("Indexing drive file alt text, please hang tight!");
            Console.WriteLine("This may take a long time (15-30 minutes), especially if your database is unusually large or you're running low end hardware.");
            migrationBuilder.Sql("""UPDATE note SET "combinedAltText"=(SELECT string_agg(comment, ' ') FROM drive_file WHERE id = ANY ("fileIds")) WHERE "fileIds" != '{}';""");

            migrationBuilder.CreateIndex(
                name: "GIN_TRGM_note_combined_alt_text",
                table: "note",
                column: "combinedAltText")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "GIN_TRGM_note_combined_alt_text",
                table: "note");

            migrationBuilder.DropColumn(
                name: "combinedAltText",
                table: "note");
        }
    }
}
