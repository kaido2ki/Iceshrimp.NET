using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace Iceshrimp.Backend.Core.Database.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(DatabaseContext))]
    [Migration("20240226234449_AddConversationsFunction")]
    public partial class AddConversationsFunction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
	        migrationBuilder.Sql("""
	                             CREATE OR REPLACE FUNCTION public.conversations(user_id character varying)
	                              RETURNS SETOF note
	                              LANGUAGE sql
	                             AS $function$ 
	                             	SELECT DISTINCT ON (COALESCE("threadId", "id")) *
	                             	FROM
	                             		"note"
	                             	WHERE
	                             		("visibility" = 'specified'	AND ("visibleUserIds" @> array["user_id"]::varchar[] OR "userId" = "user_id"))
	                             $function$;
	                             """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
	        migrationBuilder.Sql("DROP FUNCTION IF EXISTS public.conversations;");
        }
    }
}
