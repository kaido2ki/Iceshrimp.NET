using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace Iceshrimp.Backend.Core.Database.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(DatabaseContext))]
    [Migration("20240428181455_RenameDataProtectionKeysTable")]
    public partial class RenameDataProtectionKeysTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_DataProtectionKeys",
                table: "DataProtectionKeys");

            migrationBuilder.RenameTable(
                name: "DataProtectionKeys",
                newName: "data_protection_keys");

            migrationBuilder.AddPrimaryKey(
                name: "PK_data_protection_keys",
                table: "data_protection_keys",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_data_protection_keys",
                table: "data_protection_keys");

            migrationBuilder.RenameTable(
                name: "data_protection_keys",
                newName: "DataProtectionKeys");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DataProtectionKeys",
                table: "DataProtectionKeys",
                column: "Id");
        }
    }
}
