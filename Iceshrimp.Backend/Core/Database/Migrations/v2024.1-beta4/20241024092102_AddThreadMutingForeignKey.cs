using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace Iceshrimp.Backend.Core.Database.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(DatabaseContext))]
    [Migration("20241024092102_AddThreadMutingForeignKey")]
    public partial class AddThreadMutingForeignKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder
                .Sql("""DELETE FROM "note_thread_muting" WHERE "threadId" NOT IN (SELECT "id" FROM "note_thread");""");
            
            migrationBuilder.AddForeignKey(
                name: "FK_note_thread_muting_note_thread_threadId",
                table: "note_thread_muting",
                column: "threadId",
                principalTable: "note_thread",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_note_thread_muting_note_thread_threadId",
                table: "note_thread_muting");
        }
    }
}
