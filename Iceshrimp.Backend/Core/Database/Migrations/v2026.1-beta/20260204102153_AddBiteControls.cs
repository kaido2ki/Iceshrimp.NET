using Iceshrimp.Backend.Core.Database.Tables;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Iceshrimp.Backend.Core.Database.Migrations.v2025._1beta6
{
    /// <inheritdoc />
    [DbContext(typeof(DatabaseContext))]
    [Migration("20260204102153_AddBiteControls")]
    public partial class AddBiteControls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:antenna_src_enum", "home,all,users,list,group,instances")
                .Annotation("Npgsql:Enum:bite_control_enum", "public,followers")
                .Annotation("Npgsql:Enum:filter_action_enum", "warn,hide")
                .Annotation("Npgsql:Enum:filter_context_enum", "home,lists,threads,notifications,accounts,public")
                .Annotation("Npgsql:Enum:interaction_stamp_type", "quote")
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
                .OldAnnotation("Npgsql:Enum:interaction_stamp_type", "quote")
                .OldAnnotation("Npgsql:Enum:job_status", "queued,delayed,running,completed,failed")
                .OldAnnotation("Npgsql:Enum:marker_type_enum", "home,notifications")
                .OldAnnotation("Npgsql:Enum:note_visibility_enum", "public,home,followers,specified")
                .OldAnnotation("Npgsql:Enum:notification_type_enum", "follow,mention,reply,renote,quote,like,reaction,pollVote,pollEnded,receiveFollowRequest,followRequestAccepted,groupInvited,app,edit,bite,report")
                .OldAnnotation("Npgsql:Enum:page_visibility_enum", "public,followers,specified")
                .OldAnnotation("Npgsql:Enum:push_subscription_policy_enum", "all,followed,follower,none")
                .OldAnnotation("Npgsql:Enum:relay_status_enum", "requesting,accepted,rejected")
                .OldAnnotation("Npgsql:Enum:user_profile_ffvisibility_enum", "public,followers,private")
                .OldAnnotation("Npgsql:PostgresExtension:pg_trgm", ",,");

            migrationBuilder.AddColumn<User.BiteControl>(
                name: "canBite",
                table: "user",
                type: "bite_control_enum",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "canBite",
                table: "user");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:antenna_src_enum", "home,all,users,list,group,instances")
                .Annotation("Npgsql:Enum:filter_action_enum", "warn,hide")
                .Annotation("Npgsql:Enum:filter_context_enum", "home,lists,threads,notifications,accounts,public")
                .Annotation("Npgsql:Enum:interaction_stamp_type", "quote")
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
                .OldAnnotation("Npgsql:Enum:bite_control_enum", "public,followers")
                .OldAnnotation("Npgsql:Enum:filter_action_enum", "warn,hide")
                .OldAnnotation("Npgsql:Enum:filter_context_enum", "home,lists,threads,notifications,accounts,public")
                .OldAnnotation("Npgsql:Enum:interaction_stamp_type", "quote")
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
