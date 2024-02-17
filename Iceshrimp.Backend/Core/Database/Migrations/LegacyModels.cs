using NpgsqlTypes;

namespace Iceshrimp.Backend.Core.Database.Migrations;

public class LegacyModels
{
	[PgName("user_profile_mutingnotificationtypes_enum")]
	public enum MutingNotificationType
	{
		[PgName("follow")]                Follow,
		[PgName("mention")]               Mention,
		[PgName("reply")]                 Reply,
		[PgName("renote")]                Renote,
		[PgName("quote")]                 Quote,
		[PgName("reaction")]              Reaction,
		[PgName("pollVote")]              PollVote,
		[PgName("pollEnded")]             PollEnded,
		[PgName("receiveFollowRequest")]  FollowRequestReceived,
		[PgName("followRequestAccepted")] FollowRequestAccepted,
		[PgName("groupInvited")]          GroupInvited,
		[PgName("app")]                   App
	}

	[PgName("poll_notevisibility_enum")]
	public enum PollNoteVisibility
	{
		[PgName("public")]    Public,
		[PgName("home")]      Home,
		[PgName("followers")] Followers,
		[PgName("specified")] Specified,
		[PgName("hidden")]    Hidden
	}
}