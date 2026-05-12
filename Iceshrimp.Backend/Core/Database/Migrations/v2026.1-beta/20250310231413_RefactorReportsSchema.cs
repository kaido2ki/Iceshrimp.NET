using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Iceshrimp.Backend.Core.Database.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(DatabaseContext))]
    [Migration("20250310231413_RefactorReportsSchema")]
    public partial class RefactorReportsSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_abuse_user_report_user_assigneeId",
                table: "abuse_user_report");

            migrationBuilder.DropForeignKey(
                name: "FK_abuse_user_report_user_reporterId",
                table: "abuse_user_report");

            migrationBuilder.DropForeignKey(
                name: "FK_abuse_user_report_user_targetUserId",
                table: "abuse_user_report");

            migrationBuilder.DropPrimaryKey(
                name: "PK_abuse_user_report",
                table: "abuse_user_report");

            migrationBuilder.RenameTable(
                name: "abuse_user_report",
                newName: "report");

            migrationBuilder.RenameIndex(
                name: "IX_abuse_user_report_targetUserId",
                table: "report",
                newName: "IX_report_targetUserId");

            migrationBuilder.RenameIndex(
                name: "IX_abuse_user_report_targetUserHost",
                table: "report",
                newName: "IX_report_targetUserHost");

            migrationBuilder.RenameIndex(
                name: "IX_abuse_user_report_resolved",
                table: "report",
                newName: "IX_report_resolved");

            migrationBuilder.RenameIndex(
                name: "IX_abuse_user_report_reporterId",
                table: "report",
                newName: "IX_report_reporterId");

            migrationBuilder.RenameIndex(
                name: "IX_abuse_user_report_reporterHost",
                table: "report",
                newName: "IX_report_reporterHost");

            migrationBuilder.RenameIndex(
                name: "IX_abuse_user_report_createdAt",
                table: "report",
                newName: "IX_report_createdAt");

            migrationBuilder.RenameIndex(
                name: "IX_abuse_user_report_assigneeId",
                table: "report",
                newName: "IX_report_assigneeId");

            migrationBuilder.AlterColumn<DateTime>(
                name: "createdAt",
                table: "report",
                type: "timestamp with time zone",
                nullable: false,
                comment: "The created date of the Report.",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldComment: "The created date of the AbuseUserReport.");

            migrationBuilder.AddPrimaryKey(
                name: "PK_report",
                table: "report",
                column: "id");

            migrationBuilder.CreateTable(
                name: "reported_note",
                columns: table => new
                {
                    note_id = table.Column<string>(type: "character varying(32)", nullable: false),
                    report_id = table.Column<string>(type: "character varying(32)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reported_note", x => new { x.note_id, x.report_id });
                    table.ForeignKey(
                        name: "FK_reported_note_note_note_id",
                        column: x => x.note_id,
                        principalTable: "note",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_reported_note_report_report_id",
                        column: x => x.report_id,
                        principalTable: "report",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_reported_note_report_id",
                table: "reported_note",
                column: "report_id");

            migrationBuilder.AddForeignKey(
                name: "FK_report_user_assigneeId",
                table: "report",
                column: "assigneeId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_report_user_reporterId",
                table: "report",
                column: "reporterId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_report_user_targetUserId",
                table: "report",
                column: "targetUserId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_report_user_assigneeId",
                table: "report");

            migrationBuilder.DropForeignKey(
                name: "FK_report_user_reporterId",
                table: "report");

            migrationBuilder.DropForeignKey(
                name: "FK_report_user_targetUserId",
                table: "report");

            migrationBuilder.DropTable(
                name: "reported_note");

            migrationBuilder.DropPrimaryKey(
                name: "PK_report",
                table: "report");

            migrationBuilder.RenameTable(
                name: "report",
                newName: "abuse_user_report");

            migrationBuilder.RenameIndex(
                name: "IX_report_targetUserId",
                table: "abuse_user_report",
                newName: "IX_abuse_user_report_targetUserId");

            migrationBuilder.RenameIndex(
                name: "IX_report_targetUserHost",
                table: "abuse_user_report",
                newName: "IX_abuse_user_report_targetUserHost");

            migrationBuilder.RenameIndex(
                name: "IX_report_resolved",
                table: "abuse_user_report",
                newName: "IX_abuse_user_report_resolved");

            migrationBuilder.RenameIndex(
                name: "IX_report_reporterId",
                table: "abuse_user_report",
                newName: "IX_abuse_user_report_reporterId");

            migrationBuilder.RenameIndex(
                name: "IX_report_reporterHost",
                table: "abuse_user_report",
                newName: "IX_abuse_user_report_reporterHost");

            migrationBuilder.RenameIndex(
                name: "IX_report_createdAt",
                table: "abuse_user_report",
                newName: "IX_abuse_user_report_createdAt");

            migrationBuilder.RenameIndex(
                name: "IX_report_assigneeId",
                table: "abuse_user_report",
                newName: "IX_abuse_user_report_assigneeId");

            migrationBuilder.AlterColumn<DateTime>(
                name: "createdAt",
                table: "abuse_user_report",
                type: "timestamp with time zone",
                nullable: false,
                comment: "The created date of the AbuseUserReport.",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldComment: "The created date of the Report.");

            migrationBuilder.AddPrimaryKey(
                name: "PK_abuse_user_report",
                table: "abuse_user_report",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_abuse_user_report_user_assigneeId",
                table: "abuse_user_report",
                column: "assigneeId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_abuse_user_report_user_reporterId",
                table: "abuse_user_report",
                column: "reporterId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_abuse_user_report_user_targetUserId",
                table: "abuse_user_report",
                column: "targetUserId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
