using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Iceshrimp.Backend.Core.Database.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(DatabaseContext))]
    [Migration("20241022090702_AddNoteThread")]
    public partial class AddNoteThread : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            Console.WriteLine("Recomputing note thread identifiers, please hang tight!");
            Console.WriteLine("This may take a long time (15-30 minutes), especially if your database is unusually large or you're running low end hardware.");

            migrationBuilder.CreateTable(
                name: "note_thread",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    backfilledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_note_thread", x => x.id);
                });
            
            migrationBuilder.Sql("""
                                 UPDATE "note" SET "threadId"="id" WHERE "threadId" is NULL;
                                 INSERT INTO "note_thread"("id") SELECT DISTINCT "threadId" FROM "note";
                                 """);


            migrationBuilder.AlterColumn<string>(
                name: "threadId",
                table: "note",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_note_note_thread_threadId",
                table: "note",
                column: "threadId",
                principalTable: "note_thread",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_note_note_thread_threadId",
                table: "note");

            migrationBuilder.DropTable(
                name: "note_thread");

            migrationBuilder.AlterColumn<string>(
                name: "threadId",
                table: "note",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256);
        }
    }
}
