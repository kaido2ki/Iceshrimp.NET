using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("__chart__ap_request")]
[Index("Date", Name = "IDX_e56f4beac5746d44bc3e19c80d", IsUnique = true)]
[Index("Date", Name = "UQ_e56f4beac5746d44bc3e19c80d0", IsUnique = true)]
public class ChartApRequest {
	[Key] [Column("id")] public int Id { get; set; }

	[Column("date")] public int Date { get; set; }

	[Column("___deliverFailed")] public int DeliverFailed { get; set; }

	[Column("___deliverSucceeded")] public int DeliverSucceeded { get; set; }

	[Column("___inboxReceived")] public int InboxReceived { get; set; }
}