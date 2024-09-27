using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using NpgsqlTypes;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("relay")]
[Index(nameof(Inbox), IsUnique = true)]
public class Relay
{
	[PgName("relay_status_enum")]
	public enum RelayStatus
	{
		[PgName("requesting")] Requesting = 0,
		[PgName("accepted")]   Accepted = 1,
		[PgName("rejected")]   Rejected = 2
	}

	[Key]
	[Column("id")]
	[StringLength(32)]
	public string Id { get; set; } = null!;

	[Column("inbox")] [StringLength(512)] public string Inbox { get; set; } = null!;

	[Column("status")] public RelayStatus Status { get; set; }
}