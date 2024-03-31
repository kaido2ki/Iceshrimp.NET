using System;
using System.Collections.Generic;
using Iceshrimp.Backend.Core.Database.Tables;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Iceshrimp.Backend.Core.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddFilterTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:antenna_src_enum", "home,all,users,list,group,instances")
                .Annotation("Npgsql:Enum:filter_action_enum", "warn,hide")
                .Annotation("Npgsql:Enum:filter_context_enum", "home,lists,threads,notifications,accounts,public")
                .Annotation("Npgsql:Enum:job_status", "queued,delayed,running,completed,failed")
                .Annotation("Npgsql:Enum:marker_type_enum", "home,notifications")
                .Annotation("Npgsql:Enum:note_visibility_enum", "public,home,followers,specified")
                .Annotation("Npgsql:Enum:notification_type_enum", "follow,mention,reply,renote,quote,like,reaction,pollVote,pollEnded,receiveFollowRequest,followRequestAccepted,groupInvited,app,edit,bite")
                .Annotation("Npgsql:Enum:page_visibility_enum", "public,followers,specified")
                .Annotation("Npgsql:Enum:push_subscription_policy_enum", "all,followed,follower,none")
                .Annotation("Npgsql:Enum:relay_status_enum", "requesting,accepted,rejected")
                .Annotation("Npgsql:Enum:user_profile_ffvisibility_enum", "public,followers,private")
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,")
                .OldAnnotation("Npgsql:Enum:antenna_src_enum", "home,all,users,list,group,instances")
                .OldAnnotation("Npgsql:Enum:job_status", "queued,delayed,running,completed,failed")
                .OldAnnotation("Npgsql:Enum:marker_type_enum", "home,notifications")
                .OldAnnotation("Npgsql:Enum:note_visibility_enum", "public,home,followers,specified")
                .OldAnnotation("Npgsql:Enum:notification_type_enum", "follow,mention,reply,renote,quote,like,reaction,pollVote,pollEnded,receiveFollowRequest,followRequestAccepted,groupInvited,app,edit,bite")
                .OldAnnotation("Npgsql:Enum:page_visibility_enum", "public,followers,specified")
                .OldAnnotation("Npgsql:Enum:push_subscription_policy_enum", "all,followed,follower,none")
                .OldAnnotation("Npgsql:Enum:relay_status_enum", "requesting,accepted,rejected")
                .OldAnnotation("Npgsql:Enum:user_profile_ffvisibility_enum", "public,followers,private")
                .OldAnnotation("Npgsql:PostgresExtension:pg_trgm", ",,");

            migrationBuilder.CreateTable(
                name: "filter",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<string>(type: "character varying(32)", nullable: true),
                    name = table.Column<string>(type: "text", nullable: false),
                    expiry = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    keywords = table.Column<List<string>>(type: "text[]", nullable: false, defaultValueSql: "'{}'::varchar[]"),
                    contexts = table.Column<List<Filter.FilterContext>>(type: "filter_context_enum[]", nullable: false, defaultValueSql: "'{}'::public.filter_context_enum[]"),
                    action = table.Column<Filter.FilterAction>(type: "filter_action_enum", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_filter", x => x.id);
                    table.ForeignKey(
                        name: "FK_filter_user_user_id",
                        column: x => x.user_id,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_filter_user_id",
                table: "filter",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "filter");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:antenna_src_enum", "home,all,users,list,group,instances")
                .Annotation("Npgsql:Enum:job_status", "queued,delayed,running,completed,failed")
                .Annotation("Npgsql:Enum:marker_type_enum", "home,notifications")
                .Annotation("Npgsql:Enum:note_visibility_enum", "public,home,followers,specified")
                .Annotation("Npgsql:Enum:notification_type_enum", "follow,mention,reply,renote,quote,like,reaction,pollVote,pollEnded,receiveFollowRequest,followRequestAccepted,groupInvited,app,edit,bite")
                .Annotation("Npgsql:Enum:page_visibility_enum", "public,followers,specified")
                .Annotation("Npgsql:Enum:push_subscription_policy_enum", "all,followed,follower,none")
                .Annotation("Npgsql:Enum:relay_status_enum", "requesting,accepted,rejected")
                .Annotation("Npgsql:Enum:user_profile_ffvisibility_enum", "public,followers,private")
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,")
                .OldAnnotation("Npgsql:Enum:antenna_src_enum", "home,all,users,list,group,instances")
                .OldAnnotation("Npgsql:Enum:filter_action_enum", "warn,hide")
                .OldAnnotation("Npgsql:Enum:filter_context_enum", "home,lists,threads,notifications,accounts,public")
                .OldAnnotation("Npgsql:Enum:job_status", "queued,delayed,running,completed,failed")
                .OldAnnotation("Npgsql:Enum:marker_type_enum", "home,notifications")
                .OldAnnotation("Npgsql:Enum:note_visibility_enum", "public,home,followers,specified")
                .OldAnnotation("Npgsql:Enum:notification_type_enum", "follow,mention,reply,renote,quote,like,reaction,pollVote,pollEnded,receiveFollowRequest,followRequestAccepted,groupInvited,app,edit,bite")
                .OldAnnotation("Npgsql:Enum:page_visibility_enum", "public,followers,specified")
                .OldAnnotation("Npgsql:Enum:push_subscription_policy_enum", "all,followed,follower,none")
                .OldAnnotation("Npgsql:Enum:relay_status_enum", "requesting,accepted,rejected")
                .OldAnnotation("Npgsql:Enum:user_profile_ffvisibility_enum", "public,followers,private")
                .OldAnnotation("Npgsql:PostgresExtension:pg_trgm", ",,");
        }
    }
}
