using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace Iceshrimp.Backend.Core.Database.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(DatabaseContext))]
    [Migration("20241022190834_AddJobMutexColumn")]
    public partial class AddJobMutexColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "mutex",
                table: "jobs",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_jobs_mutex",
                table: "jobs",
                column: "mutex",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_jobs_mutex",
                table: "jobs");

            migrationBuilder.DropColumn(
                name: "mutex",
                table: "jobs");
        }
    }
}
