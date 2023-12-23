using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("__chart_day__active_users")]
[Index("Date", Name = "IDX_d5954f3df5e5e3bdfc3c03f390", IsUnique = true)]
[Index("Date", Name = "UQ_d5954f3df5e5e3bdfc3c03f3906", IsUnique = true)]
public class ChartDayActiveUser {
	[Key] [Column("id")] public int Id { get; set; }

	[Column("date")] public int Date { get; set; }

	[Column("unique_temp___registeredWithinWeek", TypeName = "character varying[]")]
	public List<string> UniqueTempRegisteredWithinWeek { get; set; } = null!;

	[Column("___registeredWithinWeek")] public short RegisteredWithinWeek { get; set; }

	[Column("unique_temp___registeredWithinMonth", TypeName = "character varying[]")]
	public List<string> UniqueTempRegisteredWithinMonth { get; set; } = null!;

	[Column("___registeredWithinMonth")] public short RegisteredWithinMonth { get; set; }

	[Column("unique_temp___registeredWithinYear", TypeName = "character varying[]")]
	public List<string> UniqueTempRegisteredWithinYear { get; set; } = null!;

	[Column("___registeredWithinYear")] public short RegisteredWithinYear { get; set; }

	[Column("unique_temp___registeredOutsideWeek", TypeName = "character varying[]")]
	public List<string> UniqueTempRegisteredOutsideWeek { get; set; } = null!;

	[Column("___registeredOutsideWeek")] public short RegisteredOutsideWeek { get; set; }

	[Column("unique_temp___registeredOutsideMonth", TypeName = "character varying[]")]
	public List<string> UniqueTempRegisteredOutsideMonth { get; set; } = null!;

	[Column("___registeredOutsideMonth")] public short RegisteredOutsideMonth { get; set; }

	[Column("unique_temp___registeredOutsideYear", TypeName = "character varying[]")]
	public List<string> UniqueTempRegisteredOutsideYear { get; set; } = null!;

	[Column("___registeredOutsideYear")] public short RegisteredOutsideYear { get; set; }

	[Column("___readWrite")] public short ReadWrite { get; set; }

	[Column("unique_temp___read", TypeName = "character varying[]")]
	public List<string> UniqueTempRead { get; set; } = null!;

	[Column("___read")] public short Read { get; set; }

	[Column("unique_temp___write", TypeName = "character varying[]")]
	public List<string> UniqueTempWrite { get; set; } = null!;

	[Column("___write")] public short Write { get; set; }
}