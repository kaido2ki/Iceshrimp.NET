using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Iceshrimp.Backend.Core.Database.Migrations
{
    /// <inheritdoc />
    public partial class UserAlsoKnownAsToArray : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
	        migrationBuilder.AddColumn<List<string>>(name: "alsoKnownAs_new",
	                                                 table: "user",
	                                                 type: "text[]",
	                                                 nullable: true,
	                                                 comment: "URIs the user is known as too");

	        migrationBuilder.Sql("""
	                             UPDATE "user" SET "alsoKnownAs_new" = string_to_array("alsoKnownAs", ',') WHERE "alsoKnownAs" IS NOT NULL;
	                             ALTER TABLE "user" DROP COLUMN "alsoKnownAs";
	                             ALTER TABLE "user" RENAME COLUMN "alsoKnownAs_new" TO "alsoKnownAs";
	                             """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        { 
	        migrationBuilder.AddColumn<string>(name: "alsoKnownAs_new", 
	                                           table: "user", 
	                                           type: "text", 
	                                           nullable: true, 
	                                           comment: "URIs the user is known as too");
	        
	        migrationBuilder.Sql("""
	                             UPDATE "user" SET "alsoKnownAs_new" = array_to_string("alsoKnownAs", ',') WHERE "alsoKnownAs" IS NOT NULL;
	                             ALTER TABLE "user" DROP COLUMN "alsoKnownAs";
	                             ALTER TABLE "user" RENAME COLUMN "alsoKnownAs_new" TO "alsoKnownAs";
	                             """);
        }
    }
}
