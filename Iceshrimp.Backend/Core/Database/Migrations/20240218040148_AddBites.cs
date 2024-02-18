using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Iceshrimp.Backend.Core.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddBites : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:antenna_src_enum", "home,all,users,list,group,instances")
                .Annotation("Npgsql:Enum:note_visibility_enum", "public,home,followers,specified")
                .Annotation("Npgsql:Enum:notification_type_enum", "follow,mention,reply,renote,quote,like,reaction,pollVote,pollEnded,receiveFollowRequest,followRequestAccepted,groupInvited,app,edit,bite")
                .Annotation("Npgsql:Enum:page_visibility_enum", "public,followers,specified")
                .Annotation("Npgsql:Enum:relay_status_enum", "requesting,accepted,rejected")
                .Annotation("Npgsql:Enum:user_profile_ffvisibility_enum", "public,followers,private")
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,")
                .OldAnnotation("Npgsql:Enum:antenna_src_enum", "home,all,users,list,group,instances")
                .OldAnnotation("Npgsql:Enum:note_visibility_enum", "public,home,followers,specified")
                .OldAnnotation("Npgsql:Enum:notification_type_enum", "follow,mention,reply,renote,quote,like,reaction,pollVote,pollEnded,receiveFollowRequest,followRequestAccepted,groupInvited,app,edit")
                .OldAnnotation("Npgsql:Enum:page_visibility_enum", "public,followers,specified")
                .OldAnnotation("Npgsql:Enum:relay_status_enum", "requesting,accepted,rejected")
                .OldAnnotation("Npgsql:Enum:user_profile_ffvisibility_enum", "public,followers,private")
                .OldAnnotation("Npgsql:PostgresExtension:pg_trgm", ",,");

            migrationBuilder.AddColumn<string>(
                name: "biteId",
                table: "notification",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "bite",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    createdAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    uri = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    userId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    userHost = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    targetUserId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    targetNoteId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    targetBiteId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bite", x => x.id);
                    table.ForeignKey(
                        name: "FK_bite_bite_targetBiteId",
                        column: x => x.targetBiteId,
                        principalTable: "bite",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_bite_note_targetNoteId",
                        column: x => x.targetNoteId,
                        principalTable: "note",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_bite_user_targetUserId",
                        column: x => x.targetUserId,
                        principalTable: "user",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_bite_user_userId",
                        column: x => x.userId,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_notification_biteId",
                table: "notification",
                column: "biteId");

            migrationBuilder.CreateIndex(
                name: "IX_bite_targetBiteId",
                table: "bite",
                column: "targetBiteId");

            migrationBuilder.CreateIndex(
                name: "IX_bite_targetNoteId",
                table: "bite",
                column: "targetNoteId");

            migrationBuilder.CreateIndex(
                name: "IX_bite_targetUserId",
                table: "bite",
                column: "targetUserId");

            migrationBuilder.CreateIndex(
                name: "IX_bite_uri",
                table: "bite",
                column: "uri");

            migrationBuilder.CreateIndex(
                name: "IX_bite_userHost",
                table: "bite",
                column: "userHost");

            migrationBuilder.CreateIndex(
                name: "IX_bite_userId",
                table: "bite",
                column: "userId");

            migrationBuilder.AddForeignKey(
                name: "FK_notification_bite_biteId",
                table: "notification",
                column: "biteId",
                principalTable: "bite",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_notification_bite_biteId",
                table: "notification");

            migrationBuilder.DropTable(
                name: "bite");

            migrationBuilder.DropIndex(
                name: "IX_notification_biteId",
                table: "notification");

            migrationBuilder.DropColumn(
                name: "biteId",
                table: "notification");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:antenna_src_enum", "home,all,users,list,group,instances")
                .Annotation("Npgsql:Enum:note_visibility_enum", "public,home,followers,specified")
                .Annotation("Npgsql:Enum:notification_type_enum", "follow,mention,reply,renote,quote,like,reaction,pollVote,pollEnded,receiveFollowRequest,followRequestAccepted,groupInvited,app,edit")
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
        }
    }
}
