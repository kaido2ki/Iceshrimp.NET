using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("__chart__federation")]
[Index("Date", Name = "IDX_36cb699c49580d4e6c2e6159f9", IsUnique = true)]
[Index("Date", Name = "UQ_36cb699c49580d4e6c2e6159f97", IsUnique = true)]
public class ChartFederation {
	[Key] [Column("id")] public int Id { get; set; }

	[Column("date")] public int Date { get; set; }

	[Column("unique_temp___deliveredInstances", TypeName = "character varying[]")]
	public List<string> UniqueTempDeliveredInstances { get; set; } = null!;

	[Column("___deliveredInstances")] public short DeliveredInstances { get; set; }

	[Column("unique_temp___inboxInstances", TypeName = "character varying[]")]
	public List<string> UniqueTempInboxInstances { get; set; } = null!;

	[Column("___inboxInstances")] public short InboxInstances { get; set; }

	[Column("unique_temp___stalled", TypeName = "character varying[]")]
	public List<string> UniqueTempStalled { get; set; } = null!;

	[Column("___stalled")] public short Stalled { get; set; }

	[Column("___sub")] public short Sub { get; set; }

	[Column("___pub")] public short Pub { get; set; }

	[Column("___pubsub")] public short Pubsub { get; set; }

	[Column("___subActive")] public short SubActive { get; set; }

	[Column("___pubActive")] public short PubActive { get; set; }
}