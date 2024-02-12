using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Iceshrimp.Backend.Core.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddDatabaseFunctions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
	        migrationBuilder.Sql("""
	                             DROP FUNCTION IF EXISTS public.note_ancestors;
	                             CREATE FUNCTION public.note_ancestors (start_id character varying, max_depth integer)
	                             	RETURNS SETOF "note"
	                             	LANGUAGE sql
	                             	AS $function$
	                             	SELECT
	                             		*
	                             	FROM
	                             		"note"
	                             	WHERE
	                             		"id" IN( SELECT DISTINCT
	                             				id FROM (WITH RECURSIVE ancestors AS (
	                             						SELECT
	                             							"id",
	                             							"replyId" FROM "note"
	                             						WHERE
	                             							"id" = "start_id"
	                             						UNION ALL
	                             						SELECT
	                             							"note"."id",
	                             							"note"."replyId" FROM "note"
	                             							JOIN ancestors ON ancestors. "replyId" = "note"."id"
	                             )
	                             SELECT
	                             	id FROM ancestors
	                             LIMIT max_depth + 1) AS RECURSIVE
	                             WHERE
	                             	"id" != start_id
	                             ORDER BY
	                             	"id" ASC)
	                             $function$;
	                             """);
	        
	        migrationBuilder.Sql("""
	                             DROP FUNCTION IF EXISTS public.note_descendants;
	                             CREATE FUNCTION public.note_descendants (start_id character varying, max_depth integer, max_breadth integer)
	                             	RETURNS SETOF "note"
	                             	LANGUAGE sql
	                             	AS $function$
	                             	SELECT
	                             		*
	                             	FROM
	                             		"note"
	                             	WHERE
	                             		id IN( SELECT DISTINCT
	                             				id FROM (WITH RECURSIVE tree (
	                             						id,
	                             						ancestors,
	                             						depth
	                             ) AS (
	                             						SELECT
	                             							start_id,
	                             							'{}'::VARCHAR [],
	                             							0
	                             						UNION
	                             						SELECT
	                             							note.id,
	                             							CASE WHEN note. "replyId" = tree.id THEN
	                             								tree.ancestors || note. "replyId"
	                             							ELSE
	                             								tree.ancestors || note. "renoteId"
	                             							END,
	                             							depth + 1 FROM note,
	                             							tree
	                             						WHERE (note. "replyId" = tree.id
	                             							OR(
	                             								-- get renotes but not pure renotes
	                             								note. "renoteId" = tree.id
	                             								AND(note.text IS NOT NULL
	                             									OR CARDINALITY (note. "fileIds"
	                             ) != 0
	                             	OR note. "hasPoll" = TRUE)))
	                             AND depth < max_depth)
	                             SELECT
	                             	id,
	                             	-- apply the limit per node
	                             	row_number() OVER (PARTITION BY ancestors [array_upper(ancestors, 1)]) AS nth_child FROM tree
	                             WHERE
	                             	depth > 0) AS RECURSIVE
	                             WHERE
	                             	nth_child < max_breadth)
	                             $function$;
	                             """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
	        migrationBuilder.Sql("""
	                             DROP FUNCTION IF EXISTS public.note_ancestors;
	                             CREATE FUNCTION public.note_ancestors (start_id character varying, max_depth integer)
	                             	RETURNS TABLE (
	                             		id character varying)
	                             	LANGUAGE sql
	                             	AS $function$
	                             	SELECT DISTINCT
	                             		id
	                             	FROM (WITH RECURSIVE ancestors AS (
	                             			SELECT
	                             				"id",
	                             				"replyId"
	                             			FROM
	                             				"note"
	                             			WHERE
	                             				"id" = "start_id"
	                             			UNION ALL
	                             			SELECT
	                             				"note"."id",
	                             				"note"."replyId"
	                             			FROM
	                             				"note"
	                             				JOIN ancestors ON ancestors. "replyId" = "note"."id"
	                             )
	                             		SELECT
	                             			id
	                             		FROM
	                             			ancestors
	                             		LIMIT max_depth + 1
	                             ) AS RECURSIVE
	                             WHERE
	                             	"id" != start_id
	                             ORDER BY
	                             	"id" ASC
	                             $function$;
	                             """);
	        
	        migrationBuilder.Sql("""
	                             DROP FUNCTION IF EXISTS public.note_descendants;
	                             CREATE FUNCTION public.note_descendants (start_id character varying, max_depth integer, max_breadth integer)
	                             	RETURNS TABLE (
	                             		id character varying)
	                             	LANGUAGE sql
	                             	AS $function$
	                             	SELECT DISTINCT
	                             		id
	                             	FROM (WITH RECURSIVE tree (
	                             			id,
	                             			ancestors,
	                             			depth
	                             ) AS (
	                             			SELECT
	                             				start_id,
	                             				'{}'::VARCHAR [],
	                             				0
	                             			UNION
	                             			SELECT
	                             				note.id,
	                             				CASE WHEN note. "replyId" = tree.id THEN
	                             					tree.ancestors || note. "replyId"
	                             				ELSE
	                             					tree.ancestors || note. "renoteId"
	                             				END,
	                             				depth + 1
	                             			FROM
	                             				note,
	                             				tree
	                             			WHERE (note. "replyId" = tree.id
	                             				OR(
	                             					-- get renotes but not pure renotes
	                             					note. "renoteId" = tree.id
	                             					AND(note.text IS NOT NULL
	                             						OR CARDINALITY (note. "fileIds") != 0
	                             						OR note. "hasPoll" = TRUE)))
	                             			AND depth < max_depth
	                             )
	                             	SELECT
	                             		id,
	                             		-- apply the limit per node
	                             		row_number() OVER (PARTITION BY ancestors [array_upper(ancestors, 1)]) AS nth_child
	                             	FROM
	                             		tree
	                             	WHERE
	                             		depth > 0
	                             ) AS RECURSIVE
	                             WHERE
	                             	nth_child < max_breadth
	                             $function$;
	                             """);
        }
    }
}
