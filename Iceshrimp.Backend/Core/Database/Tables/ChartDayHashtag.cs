using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("__chart_day__hashtag")]
[Index("Date", "Group", Name = "IDX_8f589cf056ff51f09d6096f645", IsUnique = true)]
[Index("Date", "Group", Name = "UQ_8f589cf056ff51f09d6096f6450", IsUnique = true)]
public class ChartDayHashtag {
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