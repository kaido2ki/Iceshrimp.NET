using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace Iceshrimp.Backend.Core.Database.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(DatabaseContext))]
    [Migration("20250923085359_AddReportNotification")]
    public partial class AddReportNotification : Migration
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
                .Annotation("Npgsql:Enum:notification_type_enum", "follow,mention,reply,renote,quote,like,reaction,pollVote,pollEnded,receiveFollowRequest,followRequestAccepted,groupInvited,app,edit,bite,report")
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

            migrationBuilder.AddColumn<string>(
                name: "reportId",
                table: "notification",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_notification_reportId",
                table: "notification",
                column: "reportId");

            migrationBuilder.AddForeignKey(
                name: "FK_notification_report_reportId",
                table: "notification",
                column: "reportId",
                principalTable: "report",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_notification_report_reportId",
                table: "notification");

            migrationBuilder.DropIndex(
                name: "IX_notification_reportId",
                table: "notification");

            migrationBuilder.DropColumn(
                name: "reportId",
                table: "notification");

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
                .OldAnnotation("Npgsql:Enum:filter_action_enum", "warn,hide")
                .OldAnnotation("Npgsql:Enum:filter_context_enum", "home,lists,threads,notifications,accounts,public")
                .OldAnnotation("Npgsql:Enum:job_status", "queued,delayed,running,completed,failed")
                .OldAnnotation("Npgsql:Enum:marker_type_enum", "home,notifications")
                .OldAnnotation("Npgsql:Enum:note_visibility_enum", "public,home,followers,specified")
                .OldAnnotation("Npgsql:Enum:notification_type_enum", "follow,mention,reply,renote,quote,like,reaction,pollVote,pollEnded,receiveFollowRequest,followRequestAccepted,groupInvited,app,edit,bite,report")
                .OldAnnotation("Npgsql:Enum:page_visibility_enum", "public,followers,specified")
                .OldAnnotation("Npgsql:Enum:push_subscription_policy_enum", "all,followed,follower,none")
                .OldAnnotation("Npgsql:Enum:relay_status_enum", "requesting,accepted,rejected")
                .OldAnnotation("Npgsql:Enum:user_profile_ffvisibility_enum", "public,followers,private")
                .OldAnnotation("Npgsql:PostgresExtension:pg_trgm", ",,");
        }
    }
}
