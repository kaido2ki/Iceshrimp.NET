using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("__chart__users")]
[Index("Date", Name = "IDX_845254b3eaf708ae8a6cac3026", IsUnique = true)]
[Index("Date", Name = "UQ_845254b3eaf708ae8a6cac30265", IsUnique = true)]
public class ChartUser {
	[Key] [Column("id")] public int Id { get; set; }

	[Column("date")] public int Date { get; set; }

	[Column("___local_total")] public int LocalTotal { get; set; }

	[Column("___local_inc")] public short LocalInc { get; set; }

	[Column("___local_dec")] public short LocalDec { get; set; }

	[Column("___remote_total")] public int RemoteTotal { get; set; }

	[Column("___remote_inc")] public short RemoteInc { get; set; }

	[Column("___remote_dec")] public short RemoteDec { get; set; }
}