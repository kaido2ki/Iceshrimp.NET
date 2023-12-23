using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("__chart_day__drive")]
[Index("Date", Name = "IDX_0b60ebb3aa0065f10b0616c117", IsUnique = true)]
[Index("Date", Name = "UQ_0b60ebb3aa0065f10b0616c1171", IsUnique = true)]
public class ChartDayDrive {
	[Key] [Column("id")] public int Id { get; set; }

	[Column("date")] public int Date { get; set; }

	[Column("___local_incCount")] public int LocalIncCount { get; set; }

	[Column("___local_incSize")] public int LocalIncSize { get; set; }

	[Column("___local_decCount")] public int LocalDecCount { get; set; }

	[Column("___local_decSize")] public int LocalDecSize { get; set; }

	[Column("___remote_incCount")] public int RemoteIncCount { get; set; }

	[Column("___remote_incSize")] public int RemoteIncSize { get; set; }

	[Column("___remote_decCount")] public int RemoteDecCount { get; set; }

	[Column("___remote_decSize")] public int RemoteDecSize { get; set; }
}