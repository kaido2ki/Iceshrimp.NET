using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("__chart_day__network")]
[Index("Date", Name = "IDX_8bfa548c2b31f9e07db113773e", IsUnique = true)]
[Index("Date", Name = "UQ_8bfa548c2b31f9e07db113773ee", IsUnique = true)]
public class ChartDayNetwork {
	[Key] [Column("id")] public int Id { get; set; }

	[Column("date")] public int Date { get; set; }

	[Column("___incomingRequests")] public int IncomingRequests { get; set; }

	[Column("___outgoingRequests")] public int OutgoingRequests { get; set; }

	[Column("___totalTime")] public int TotalTime { get; set; }

	[Column("___incomingBytes")] public int IncomingBytes { get; set; }

	[Column("___outgoingBytes")] public int OutgoingBytes { get; set; }
}