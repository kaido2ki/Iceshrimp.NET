using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("__chart__hashtag")]
[Index("Date", "Group", Name = "IDX_25a97c02003338124b2b75fdbc", IsUnique = true)]
[Index("Date", "Group", Name = "UQ_25a97c02003338124b2b75fdbc8", IsUnique = true)]
public class ChartHashtag {
	[Key] [Column("id")] public int Id { get; set; }

	[Column("date")] public int Date { get; set; }

	[Column("group")] [StringLength(128)] public string Group { get; set; } = null!;

	[Column("___local_users")] public int LocalUsers { get; set; }

	[Column("___remote_users")] public int RemoteUsers { get; set; }

	[Column("unique_temp___local_users", TypeName = "character varying[]")]
	public List<string> UniqueTempLocalUsers { get; set; } = null!;

	[Column("unique_temp___remote_users", TypeName = "character varying[]")]
	public List<string> UniqueTempRemoteUsers { get; set; } = null!;
}