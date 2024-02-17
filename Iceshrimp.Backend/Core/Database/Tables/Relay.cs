using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using NpgsqlTypes;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("relay")]
[Index("Inbox", IsUnique = true)]
public class Relay
{
	[PgName("relay_status_enum")]
	public enum RelayStatus
	{
		[PgName("requesting")] Requesting,
		[PgName("accepted")]   Accepted,
		[PgName("rejected")]   Rejected
	}

	[Key]
	[Column("id")]
	[StringLength(32)]
	public string Id { get; set; } = null!;

	[Column("inbox")] [StringLength(512)] public string Inbox { get; set; } = null!;

	[Column("status")] public RelayStatus Status { get; set; }
}