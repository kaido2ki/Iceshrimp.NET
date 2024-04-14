using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace Iceshrimp.Backend.Core.Database.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(DatabaseContext))]
    [Migration("20240326153635_AddJobWorkerSetNullConstraint")]
    public partial class AddJobWorkerSetNullConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_jobs_worker_worker_id",
                table: "jobs");

            migrationBuilder.AddForeignKey(
                name: "FK_jobs_worker_worker_id",
                table: "jobs",
                column: "worker_id",
                principalTable: "worker",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_jobs_worker_worker_id",
                table: "jobs");

            migrationBuilder.AddForeignKey(
                name: "FK_jobs_worker_worker_id",
                table: "jobs",
                column: "worker_id",
                principalTable: "worker",
                principalColumn: "id");
        }
    }
}
