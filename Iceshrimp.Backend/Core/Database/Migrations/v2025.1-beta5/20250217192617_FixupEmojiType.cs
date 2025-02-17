using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace Iceshrimp.Backend.Core.Database.Migrations
{
	/// <inheritdoc />
    [DbContext(typeof(DatabaseContext))]
    [Migration("20250217192617_FixupEmojiType")]
	public partial class FixupEmojiType : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.Sql("""
			                     UPDATE "emoji"
			                     SET "type" =
			                         (SELECT COALESCE("drive_file"."webpublicType", "drive_file"."type")
			                          FROM "drive_file"
			                          WHERE "drive_file"."userHost" IS NULL
			                            AND ("drive_file"."webpublicUrl" = "emoji"."publicUrl"
			                                     OR "drive_file"."url" = "emoji"."publicUrl"
			                                )
			                          )
			                     WHERE "host" IS NULL
			                       AND EXISTS
			                           (SELECT 1
			                            FROM "drive_file"
			                            WHERE "drive_file"."userHost" IS NULL
			                              AND ("drive_file"."webpublicUrl" = "emoji"."publicUrl"
			                                       OR "drive_file"."url" = "emoji"."publicUrl"
			                                  )
			                            );
			                     """);
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder) { }
	}
}
