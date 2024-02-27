using System;
using Iceshrimp.Backend.Core.Database.Tables;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Iceshrimp.Backend.Core.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddMarkerTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:antenna_src_enum", "home,all,users,list,group,instances")
                .Annotation("Npgsql:Enum:marker_type_enum", "home,notifications")
                .Annotation("Npgsql:Enum:note_visibility_enum", "public,home,followers,specified")
                .Annotation("Npgsql:Enum:notification_type_enum", "follow,mention,reply,renote,quote,like,reaction,pollVote,pollEnded,receiveFollowRequest,followRequestAccepted,groupInvited,app,edit,bite")
                .Annotation("Npgsql:Enum:page_visibility_enum", "public,followers,specified")
                .Annotation("Npgsql:Enum:relay_status_enum", "requesting,accepted,rejected")
                .Annotation("Npgsql:Enum:user_profile_ffvisibility_enum", "public,followers,private")
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,")
                .OldAnnotation("Npgsql:Enum:antenna_src_enum", "home,all,users,list,group,instances")
                .OldAnnotation("Npgsql:Enum:note_visibility_enum", "public,home,followers,specified")
                .OldAnnotation("Npgsql:Enum:notification_type_enum", "follow,mention,reply,renote,quote,like,reaction,pollVote,pollEnded,receiveFollowRequest,followRequestAccepted,groupInvited,app,edit,bite")
                .OldAnnotation("Npgsql:Enum:page_visibility_enum", "public,followers,specified")
                .OldAnnotation("Npgsql:Enum:relay_status_enum", "requesting,accepted,rejected")
                .OldAnnotation("Npgsql:Enum:user_profile_ffvisibility_enum", "public,followers,private")
                .OldAnnotation("Npgsql:PostgresExtension:pg_trgm", ",,");

            migrationBuilder.CreateTable(
                name: "marker",
                columns: table => new
                {
                    userId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    type = table.Column<Marker.MarkerType>(type: "marker_type_enum", maxLength: 32, nullable: false),
                    position = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    lastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_marker", x => new { x.userId, x.type });
                    table.ForeignKey(
                        name: "FK_marker_user_userId",
                        column: x => x.userId,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_marker_userId",
                table: "marker",
                column: "userId");

            migrationBuilder.CreateIndex(
                name: "IX_marker_userId_type",
                table: "marker",
                columns: new[] { "userId", "type" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "marker");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:antenna_src_enum", "home,all,users,list,group,instances")
                .Annotation("Npgsql:Enum:note_visibility_enum", "public,home,followers,specified")
                .Annotation("Npgsql:Enum:notification_type_enum", "follow,mention,reply,renote,quote,like,reaction,pollVote,pollEnded,receiveFollowRequest,followRequestAccepted,groupInvited,app,edit,bite")
                .Annotation("Npgsql:Enum:page_visibility_enum", "public,followers,specified")
                .Annotation("Npgsql:Enum:relay_status_enum", "requesting,accepted,rejected")
                .Annotation("Npgsql:Enum:user_profile_ffvisibility_enum", "public,followers,private")
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,")
                .OldAnnotation("Npgsql:Enum:antenna_src_enum", "home,all,users,list,group,instances")
                .OldAnnotation("Npgsql:Enum:marker_type_enum", "home,notifications")
                .OldAnnotation("Npgsql:Enum:note_visibility_enum", "public,home,followers,specified")
                .OldAnnotation("Npgsql:Enum:notification_type_enum", "follow,mention,reply,renote,quote,like,reaction,pollVote,pollEnded,receiveFollowRequest,followRequestAccepted,groupInvited,app,edit,bite")
                .OldAnnotation("Npgsql:Enum:page_visibility_enum", "public,followers,specified")
                .OldAnnotation("Npgsql:Enum:relay_status_enum", "requesting,accepted,rejected")
                .OldAnnotation("Npgsql:Enum:user_profile_ffvisibility_enum", "public,followers,private")
                .OldAnnotation("Npgsql:PostgresExtension:pg_trgm", ",,");
        }
    }
}
