using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Iceshrimp.Backend.Core.Database.Tables;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Iceshrimp.Backend.Core.Database.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(DatabaseContext))]
    [Migration("20240204220725_MergePostgresEnums")]
    public partial class MergePostgresEnums : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) {
	        migrationBuilder.Sql("""
	                             ALTER TABLE "poll" ALTER COLUMN "noteVisibility" TYPE "public"."note_visibility_enum" USING "noteVisibility"::"text"::"public"."note_visibility_enum";
	                             ALTER TABLE "user_profile" ALTER COLUMN "mutingNotificationTypes" DROP DEFAULT;
	                             ALTER TABLE "user_profile" ALTER COLUMN "mutingNotificationTypes" TYPE "public"."notification_type_enum"[] USING "mutingNotificationTypes"::"text"::"public"."notification_type_enum"[];
	                             ALTER TABLE "user_profile" ALTER COLUMN "mutingNotificationTypes" SET DEFAULT '{}';
	                             """);
	        
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:antenna_src_enum", "home,all,users,list,group,instances")
                .Annotation("Npgsql:Enum:note_visibility_enum", "public,home,followers,specified")
                .Annotation("Npgsql:Enum:notification_type_enum", "follow,mention,reply,renote,quote,reaction,pollVote,pollEnded,receiveFollowRequest,followRequestAccepted,groupInvited,app")
                .Annotation("Npgsql:Enum:page_visibility_enum", "public,followers,specified")
                .Annotation("Npgsql:Enum:relay_status_enum", "requesting,accepted,rejected")
                .Annotation("Npgsql:Enum:user_profile_ffvisibility_enum", "public,followers,private")
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,")
                .OldAnnotation("Npgsql:Enum:antenna_src_enum", "home,all,users,list,group,instances")
                .OldAnnotation("Npgsql:Enum:note_visibility_enum", "public,home,followers,specified")
                .OldAnnotation("Npgsql:Enum:notification_type_enum", "follow,mention,reply,renote,quote,reaction,pollVote,pollEnded,receiveFollowRequest,followRequestAccepted,groupInvited,app")
                .OldAnnotation("Npgsql:Enum:page_visibility_enum", "public,followers,specified")
                .OldAnnotation("Npgsql:Enum:poll_notevisibility_enum", "public,home,followers,specified")
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
                .Annotation("Npgsql:Enum:note_visibility_enum", "public,home,followers,specified")
                .Annotation("Npgsql:Enum:notification_type_enum", "follow,mention,reply,renote,quote,reaction,pollVote,pollEnded,receiveFollowRequest,followRequestAccepted,groupInvited,app")
                .Annotation("Npgsql:Enum:page_visibility_enum", "public,followers,specified")
                .Annotation("Npgsql:Enum:poll_notevisibility_enum", "public,home,followers,specified")
                .Annotation("Npgsql:Enum:relay_status_enum", "requesting,accepted,rejected")
                .Annotation("Npgsql:Enum:user_profile_ffvisibility_enum", "public,followers,private")
                .Annotation("Npgsql:Enum:user_profile_mutingnotificationtypes_enum", "follow,mention,reply,renote,quote,reaction,pollVote,pollEnded,receiveFollowRequest,followRequestAccepted,groupInvited,app")
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,")
                .OldAnnotation("Npgsql:Enum:antenna_src_enum", "home,all,users,list,group,instances")
                .OldAnnotation("Npgsql:Enum:note_visibility_enum", "public,home,followers,specified")
                .OldAnnotation("Npgsql:Enum:notification_type_enum", "follow,mention,reply,renote,quote,reaction,pollVote,pollEnded,receiveFollowRequest,followRequestAccepted,groupInvited,app")
                .OldAnnotation("Npgsql:Enum:page_visibility_enum", "public,followers,specified")
                .OldAnnotation("Npgsql:Enum:relay_status_enum", "requesting,accepted,rejected")
                .OldAnnotation("Npgsql:Enum:user_profile_ffvisibility_enum", "public,followers,private")
                .OldAnnotation("Npgsql:PostgresExtension:pg_trgm", ",,");
            
            migrationBuilder.Sql("""
                                 ALTER TABLE "poll" ALTER COLUMN "noteVisibility" TYPE "public"."poll_notevisibility_enum" USING "noteVisibility"::"text"::"public"."poll_notevisibility_enum";
                                 ALTER TABLE "user_profile" ALTER COLUMN "mutingNotificationTypes" DROP DEFAULT;
                                 ALTER TABLE "user_profile" ALTER COLUMN "mutingNotificationTypes" TYPE "public"."user_profile_mutingnotificationtypes_enum"[] USING "mutingNotificationTypes"::"text"::"public"."user_profile_mutingnotificationtypes_enum"[];
                                 ALTER TABLE "user_profile" ALTER COLUMN "mutingNotificationTypes" SET DEFAULT '{}';
                                 """);
        }
    }
}