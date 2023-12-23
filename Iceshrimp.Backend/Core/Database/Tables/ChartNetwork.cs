using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("__chart__network")]
[Index("Date", Name = "IDX_a1efd3e0048a5f2793a47360dc", IsUnique = true)]
[Index("Date", Name = "UQ_a1efd3e0048a5f2793a47360dc6", IsUnique = true)]
public class ChartNetwork {
	[Key] [Column("id")] public int Id { get; set; }

	[Column("date")] public int Date { get; set; }

	[Column("___incomingRequests")] public int IncomingRequests { get; set; }

	[Column("___outgoingRequests")] public int OutgoingRequests { get; set; }

	[Column("___totalTime")] public int TotalTime { get; set; }

	[Column("___incomingBytes")] public int IncomingBytes { get; set; }

	[Column("___outgoingBytes")] public int OutgoingBytes { get; set; }
}