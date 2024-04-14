using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Iceshrimp.Backend.Core.Database.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(DatabaseContext))]
    [Migration("20240326151514_AddWorkerTable")]
    public partial class AddWorkerTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "worker_id",
                table: "jobs",
                type: "character varying(64)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "worker",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    heartbeat = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_worker", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_jobs_worker_id",
                table: "jobs",
                column: "worker_id");

            migrationBuilder.CreateIndex(
                name: "IX_worker_heartbeat",
                table: "worker",
                column: "heartbeat");

            migrationBuilder.CreateIndex(
                name: "IX_worker_id",
                table: "worker",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_jobs_worker_worker_id",
                table: "jobs",
                column: "worker_id",
                principalTable: "worker",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_jobs_worker_worker_id",
                table: "jobs");

            migrationBuilder.DropTable(
                name: "worker");

            migrationBuilder.DropIndex(
                name: "IX_jobs_worker_id",
                table: "jobs");

            migrationBuilder.DropColumn(
                name: "worker_id",
                table: "jobs");
        }
    }
}
