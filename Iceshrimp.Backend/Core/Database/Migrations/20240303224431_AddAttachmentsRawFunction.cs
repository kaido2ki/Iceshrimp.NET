using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace Iceshrimp.Backend.Core.Database.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(DatabaseContext))]
    [Migration("20240303224431_AddAttachmentsRawFunction")]
    public partial class AddAttachmentsRawFunction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
	        migrationBuilder.Sql("""
	                             CREATE OR REPLACE FUNCTION public.note_attachments_raw(note_id character varying)
	                              RETURNS varchar
	                              LANGUAGE sql
	                             AS $function$ 
	                             	SELECT "attachedFileTypes"::varchar FROM "note" WHERE "id" = note_id
	                             $function$;
	                             """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
	        migrationBuilder.Sql("DROP FUNCTION public.note_attachments_raw;");
        }
    }
}
