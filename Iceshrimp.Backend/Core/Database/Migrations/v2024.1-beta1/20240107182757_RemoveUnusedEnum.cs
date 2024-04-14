using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace Iceshrimp.Backend.Core.Database.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(DatabaseContext))]
    [Migration("20240107182757_RemoveUnusedEnum")]
    public partial class RemoveUnusedEnum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:antenna_src_enum", "home,all,users,list,group,instances")
                .Annotation("Npgsql:Enum:note_visibility_enum", "public,home,followers,specified,hidden")
                .Annotation("Npgsql:Enum:notification_type_enum", "follow,mention,reply,renote,quote,reaction,pollVote,pollEnded,receiveFollowRequest,followRequestAccepted,groupInvited,app")
                .Annotation("Npgsql:Enum:page_visibility_enum", "public,followers,specified")
                .Annotation("Npgsql:Enum:poll_notevisibility_enum", "public,home,followers,specified,hidden")
                .Annotation("Npgsql:Enum:relay_status_enum", "requesting,accepted,rejected")
                .Annotation("Npgsql:Enum:user_profile_ffvisibility_enum", "public,followers,private")
                .Annotation("Npgsql:Enum:user_profile_mutingnotificationtypes_enum", "follow,mention,reply,renote,quote,reaction,pollVote,pollEnded,receiveFollowRequest,followRequestAccepted,groupInvited,app")
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,")
                .OldAnnotation("Npgsql:Enum:antenna_src_enum", "home,all,users,list,group,instances")
                .OldAnnotation("Npgsql:Enum:log_level_enum", "error,warning,info,success,debug")
                .OldAnnotation("Npgsql:Enum:note_visibility_enum", "public,home,followers,specified,hidden")
                .OldAnnotation("Npgsql:Enum:notification_type_enum", "follow,mention,reply,renote,quote,reaction,pollVote,pollEnded,receiveFollowRequest,followRequestAccepted,groupInvited,app")
                .OldAnnotation("Npgsql:Enum:page_visibility_enum", "public,followers,specified")
                .OldAnnotation("Npgsql:Enum:poll_notevisibility_enum", "public,home,followers,specified,hidden")
                .OldAnnotation("Npgsql:Enum:relay_status_enum", "requesting,accepted,rejected")
                .OldAnnotation("Npgsql:Enum:user_profile_ffvisibility_enum", "public,followers,private")
                .OldAnnotation("Npgsql:Enum:user_profile_mutingnotificationtypes_enum", "follow,mention,reply,renote,quote,reaction,pollVote,pollEnded,receiveFollowRequest,followRequestAccepted,groupInvited,app")
                .OldAnnotation("Npgsql:PostgresExtension:pg_trgm", ",,");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:antenna_src_enum", "home,all,users,list,group,instances")
                .Annotation("Npgsql:Enum:log_level_enum", "error,warning,info,success,debug")
                .Annotation("Npgsql:Enum:note_visibility_enum", "public,home,followers,specified,hidden")
                .Annotation("Npgsql:Enum:notification_type_enum", "follow,mention,reply,renote,quote,reaction,pollVote,pollEnded,receiveFollowRequest,followRequestAccepted,groupInvited,app")
                .Annotation("Npgsql:Enum:page_visibility_enum", "public,followers,specified")
                .Annotation("Npgsql:Enum:poll_notevisibility_enum", "public,home,followers,specified,hidden")
                .Annotation("Npgsql:Enum:relay_status_enum", "requesting,accepted,rejected")
                .Annotation("Npgsql:Enum:user_profile_ffvisibility_enum", "public,followers,private")
                .Annotation("Npgsql:Enum:user_profile_mutingnotificationtypes_enum", "follow,mention,reply,renote,quote,reaction,pollVote,pollEnded,receiveFollowRequest,followRequestAccepted,groupInvited,app")
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,")
                .OldAnnotation("Npgsql:Enum:antenna_src_enum", "home,all,users,list,group,instances")
                .OldAnnotation("Npgsql:Enum:note_visibility_enum", "public,home,followers,specified,hidden")
                .OldAnnotation("Npgsql:Enum:notification_type_enum", "follow,mention,reply,renote,quote,reaction,pollVote,pollEnded,receiveFollowRequest,followRequestAccepted,groupInvited,app")
                .OldAnnotation("Npgsql:Enum:page_visibility_enum", "public,followers,specified")
                .OldAnnotation("Npgsql:Enum:poll_notevisibility_enum", "public,home,followers,specified,hidden")
                .OldAnnotation("Npgsql:Enum:relay_status_enum", "requesting,accepted,rejected")
                .OldAnnotation("Npgsql:Enum:user_profile_ffvisibility_enum", "public,followers,private")
                .OldAnnotation("Npgsql:Enum:user_profile_mutingnotificationtypes_enum", "follow,mention,reply,renote,quote,reaction,pollVote,pollEnded,receiveFollowRequest,followRequestAccepted,groupInvited,app")
                .OldAnnotation("Npgsql:PostgresExtension:pg_trgm", ",,");
        }
    }
}
