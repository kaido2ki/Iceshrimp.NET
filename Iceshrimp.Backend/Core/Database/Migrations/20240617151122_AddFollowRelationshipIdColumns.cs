using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Iceshrimp.Backend.Core.Database.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(DatabaseContext))]
    [Migration("20240617151122_AddFollowRelationshipIdColumns")]
    public partial class AddFollowRelationshipIdColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "relationshipId",
                table: "following",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "relationshipId",
                table: "follow_request",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "relationshipId",
                table: "following");

            migrationBuilder.DropColumn(
                name: "relationshipId",
                table: "follow_request");
        }
    }
}
