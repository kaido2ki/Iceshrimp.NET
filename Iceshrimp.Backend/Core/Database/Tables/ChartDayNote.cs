using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("__chart_day__notes")]
[Index("Date", Name = "IDX_1a527b423ad0858a1af5a056d4", IsUnique = true)]
[Index("Date", Name = "UQ_1a527b423ad0858a1af5a056d43", IsUnique = true)]
public class ChartDayNote {
	[Key] [Column("id")] public int Id { get; set; }

	[Column("date")] public int Date { get; set; }

	[Column("___local_total")] public int LocalTotal { get; set; }

	[Column("___local_inc")] public int LocalInc { get; set; }

	[Column("___local_dec")] public int LocalDec { get; set; }

	[Column("___local_diffs_normal")] public int LocalDiffsNormal { get; set; }

	[Column("___local_diffs_reply")] public int LocalDiffsReply { get; set; }

	[Column("___local_diffs_renote")] public int LocalDiffsRenote { get; set; }

	[Column("___remote_total")] public int RemoteTotal { get; set; }

	[Column("___remote_inc")] public int RemoteInc { get; set; }

	[Column("___remote_dec")] public int RemoteDec { get; set; }

	[Column("___remote_diffs_normal")] public int RemoteDiffsNormal { get; set; }

	[Column("___remote_diffs_reply")] public int RemoteDiffsReply { get; set; }

	[Column("___remote_diffs_renote")] public int RemoteDiffsRenote { get; set; }

	[Column("___local_diffs_withFile")] public int LocalDiffsWithFile { get; set; }

	[Column("___remote_diffs_withFile")] public int RemoteDiffsWithFile { get; set; }
}