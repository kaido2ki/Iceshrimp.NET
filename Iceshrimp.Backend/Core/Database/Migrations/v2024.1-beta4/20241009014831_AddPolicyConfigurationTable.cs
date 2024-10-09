using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace Iceshrimp.Backend.Core.Database.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(DatabaseContext))]
    [Migration("20241009014831_AddPolicyConfigurationTable")]
    public partial class AddPolicyConfigurationTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "policy_configuration",
                columns: table => new
                {
                    name = table.Column<string>(type: "text", nullable: false),
                    data = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb", comment: "The plugin-specific data object")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_policy_configuration", x => x.name);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "policy_configuration");
        }
    }
}
