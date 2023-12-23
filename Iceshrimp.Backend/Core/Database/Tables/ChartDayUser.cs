using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("__chart_day__users")]
[Index("Date", Name = "IDX_cad6e07c20037f31cdba8a350c", IsUnique = true)]
[Index("Date", Name = "UQ_cad6e07c20037f31cdba8a350c3", IsUnique = true)]
public class ChartDayUser {
	[Key] [Column("id")] public int Id { get; set; }

	[Column("date")] public int Date { get; set; }

	[Column("___local_total")] public int LocalTotal { get; set; }

	[Column("___local_inc")] public short LocalInc { get; set; }

	[Column("___local_dec")] public short LocalDec { get; set; }

	[Column("___remote_total")] public int RemoteTotal { get; set; }

	[Column("___remote_inc")] public short RemoteInc { get; set; }

	[Column("___remote_dec")] public short RemoteDec { get; set; }
}