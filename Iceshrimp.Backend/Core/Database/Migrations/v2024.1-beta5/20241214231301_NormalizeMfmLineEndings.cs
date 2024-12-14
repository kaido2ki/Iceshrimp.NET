using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace Iceshrimp.Backend.Core.Database.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(DatabaseContext))]
    [Migration("20241214231301_NormalizeMfmLineEndings")]
    public partial class NormalizeMfmLineEndings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
	        migrationBuilder.Sql("""
	                             UPDATE "note" SET "text" = regexp_replace("text", '\r\n', '\n', 'g') WHERE "text" ~ '\r\n';
	                             UPDATE "note" SET "text" = regexp_replace("text", '\r', '\n', 'g') WHERE "text" ~ '\r';
	                             UPDATE "user_profile" SET "description" = regexp_replace("description", '\r\n', '\n', 'g') WHERE "description" ~ '\r\n';
	                             UPDATE "user_profile" SET "description" = regexp_replace("description", '\r', '\n', 'g') WHERE "description" ~ '\r';
	                             """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
