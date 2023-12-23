using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("__chart_day__ap_request")]
[Index("Date", Name = "IDX_a848f66d6cec11980a5dd59582", IsUnique = true)]
[Index("Date", Name = "UQ_a848f66d6cec11980a5dd595822", IsUnique = true)]
public class ChartDayApRequest {
	[Key] [Column("id")] public int Id { get; set; }

	[Column("date")] public int Date { get; set; }

	[Column("___deliverFailed")] public int DeliverFailed { get; set; }

	[Column("___deliverSucceeded")] public int DeliverSucceeded { get; set; }

	[Column("___inboxReceived")] public int InboxReceived { get; set; }
}