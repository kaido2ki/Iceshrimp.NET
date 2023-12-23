using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("__chart_day__instance")]
[Index("Date", "Group", Name = "IDX_fea7c0278325a1a2492f2d6acb", IsUnique = true)]
[Index("Date", "Group", Name = "UQ_fea7c0278325a1a2492f2d6acbf", IsUnique = true)]
public class ChartDayInstance {
	[Key] [Column("id")] public int Id { get; set; }

	[Column("date")] public int Date { get; set; }

	[Column("group")] [StringLength(128)] public string Group { get; set; } = null!;

	[Column("___requests_failed")] public short RequestsFailed { get; set; }

	[Column("___requests_succeeded")] public short RequestsSucceeded { get; set; }

	[Column("___requests_received")] public short RequestsReceived { get; set; }

	[Column("___notes_total")] public int NotesTotal { get; set; }

	[Column("___notes_inc")] public int NotesInc { get; set; }

	[Column("___notes_dec")] public int NotesDec { get; set; }

	[Column("___notes_diffs_normal")] public int NotesDiffsNormal { get; set; }

	[Column("___notes_diffs_reply")] public int NotesDiffsReply { get; set; }

	[Column("___notes_diffs_renote")] public int NotesDiffsRenote { get; set; }

	[Column("___users_total")] public int UsersTotal { get; set; }

	[Column("___users_inc")] public short UsersInc { get; set; }

	[Column("___users_dec")] public short UsersDec { get; set; }

	[Column("___following_total")] public int FollowingTotal { get; set; }

	[Column("___following_inc")] public short FollowingInc { get; set; }

	[Column("___following_dec")] public short FollowingDec { get; set; }

	[Column("___followers_total")] public int FollowersTotal { get; set; }

	[Column("___followers_inc")] public short FollowersInc { get; set; }

	[Column("___followers_dec")] public short FollowersDec { get; set; }

	[Column("___drive_totalFiles")] public int DriveTotalFiles { get; set; }

	[Column("___drive_incFiles")] public int DriveIncFiles { get; set; }

	[Column("___drive_incUsage")] public int DriveIncUsage { get; set; }

	[Column("___drive_decFiles")] public int DriveDecFiles { get; set; }

	[Column("___drive_decUsage")] public int DriveDecUsage { get; set; }

	[Column("___notes_diffs_withFile")] public int NotesDiffsWithFile { get; set; }
}