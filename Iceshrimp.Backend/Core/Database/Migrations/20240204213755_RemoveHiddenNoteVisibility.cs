using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Iceshrimp.Backend.Core.Database.Migrations
{
    /// <inheritdoc />
    public partial class RemoveHiddenNoteVisibility : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) {
	        Console.WriteLine("Fixing up note visibility enums, please hang tight!");
	        Console.WriteLine("This may take a long time (15-30 minutes), especially if your database is unusually large or you're running low end hardware.");
	        
	        // We want to set all hidden notes to be specified notes that can only be seen by the author before we remove the enum
	        migrationBuilder.Sql("""
	                             UPDATE "note" SET "visibleUserIds" = '{}', "mentions" = '{}', "visibility" = 'specified' WHERE "visibility" = 'hidden';
	                             UPDATE "poll" SET "noteVisibility" = 'specified' WHERE "noteVisibility" = 'hidden';
	                             ALTER TYPE "public"."note_visibility_enum" RENAME TO "note_visibility_enum_old";
	                             ALTER TYPE "public"."poll_notevisibility_enum" RENAME TO "poll_notevisibility_enum_old";
	                             CREATE TYPE "public"."note_visibility_enum" AS ENUM('public', 'home', 'followers', 'specified');
	                             CREATE TYPE "public"."poll_notevisibility_enum" AS ENUM('public', 'home', 'followers', 'specified');
	                             ALTER TABLE "note" ALTER COLUMN "visibility" TYPE "public"."note_visibility_enum" USING "visibility"::"text"::"public"."note_visibility_enum";
	                             ALTER TABLE "poll" ALTER COLUMN "noteVisibility" TYPE "public"."poll_notevisibility_enum" USING "noteVisibility"::"text"::"public"."poll_notevisibility_enum";
	                             DROP TYPE "public"."note_visibility_enum_old";
	                             DROP TYPE "public"."poll_notevisibility_enum_old";
	                             """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
	        Console.WriteLine("Reverting changes to note visibility enums, please hang tight!");
	        Console.WriteLine("This may take a long time (15-30 minutes), especially if your database is unusually large or you're running low end hardware.");
	        
	        migrationBuilder.Sql("""
	                             ALTER TYPE "public"."note_visibility_enum" RENAME TO "note_visibility_enum_old";
	                             ALTER TYPE "public"."poll_notevisibility_enum" RENAME TO "poll_notevisibility_enum_old";
	                             CREATE TYPE "public"."note_visibility_enum" AS ENUM('public', 'home', 'followers', 'specified', 'hidden');
	                             CREATE TYPE "public"."poll_notevisibility_enum" AS ENUM('public', 'home', 'followers', 'specified', 'hidden');
	                             ALTER TABLE "note" ALTER COLUMN "visibility" TYPE "public"."note_visibility_enum" USING "visibility"::"text"::"public"."note_visibility_enum";
	                             ALTER TABLE "poll" ALTER COLUMN "noteVisibility" TYPE "public"."poll_notevisibility_enum" USING "noteVisibility"::"text"::"public"."poll_notevisibility_enum";
	                             DROP TYPE "public"."note_visibility_enum_old";
	                             DROP TYPE "public"."poll_notevisibility_enum_old";
	                             """);
        }
    }
}
