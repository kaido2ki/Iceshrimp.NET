using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace Iceshrimp.Backend.Core.Database.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(DatabaseContext))]
    [Migration("20241214232528_NormalizeCwLineEndings")]
    public partial class NormalizeCwLineEndings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
	        migrationBuilder.Sql("""
	                             UPDATE "note" SET "text" = regexp_replace("cw", '\r\n', '\n', 'g') WHERE "cw" ~ '\r\n';
	                             UPDATE "note" SET "text" = regexp_replace("cw", '\r', '\n', 'g') WHERE "cw" ~ '\r';
	                             """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
