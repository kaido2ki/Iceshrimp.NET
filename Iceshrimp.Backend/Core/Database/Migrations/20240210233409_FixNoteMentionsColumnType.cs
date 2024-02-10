using System.Collections.Generic;
using Iceshrimp.Backend.Core.Database.Tables;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Iceshrimp.Backend.Core.Database.Migrations
{
    /// <inheritdoc />
    public partial class FixNoteMentionsColumnType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
	        migrationBuilder.Sql("""
	                             ALTER TABLE "note" ALTER COLUMN "mentionedRemoteUsers" DROP DEFAULT;
	                             ALTER TABLE "note" ALTER COLUMN "mentionedRemoteUsers" TYPE jsonb USING "mentionedRemoteUsers"::jsonb;
	                             ALTER TABLE "note" ALTER COLUMN "mentionedRemoteUsers" SET DEFAULT '[]'::jsonb;
	                             """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
	        migrationBuilder.Sql("""
	                             ALTER TABLE "note" ALTER COLUMN "mentionedRemoteUsers" DROP DEFAULT;
	                             ALTER TABLE "note" ALTER COLUMN "mentionedRemoteUsers" TYPE text USING "mentionedRemoteUsers"::text;
	                             ALTER TABLE "note" ALTER COLUMN "mentionedRemoteUsers" SET DEFAULT '[]'::text;
	                             """);
        }
    }
}
